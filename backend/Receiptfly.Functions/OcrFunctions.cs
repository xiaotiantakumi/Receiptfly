using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Receiptfly.Application.Services;
using Azure.Storage.Blobs;
using PDFtoImage;
using SkiaSharp;

namespace Receiptfly.Functions
{
    public class OcrFunctions
    {
        private readonly ILogger<OcrFunctions> _logger;
        private readonly IImageStorageService _imageStorageService;
        private readonly IOcrService _ocrService;
        private readonly IConfiguration _configuration;

        public OcrFunctions(ILogger<OcrFunctions> logger, IImageStorageService imageStorageService, IOcrService ocrService, IConfiguration configuration)
        {
            _logger = logger;
            _imageStorageService = imageStorageService;
            _ocrService = ocrService;
            _configuration = configuration;
        }

        [Function("ProcessImage")]
        public async Task<IActionResult> ProcessImage([HttpTrigger(AuthorizationLevel.Function, "post", Route = "ocr")] HttpRequest req)
        {
            if (!req.HasFormContentType)
            {
                return new BadRequestObjectResult("Content type must be multipart/form-data");
            }

            var form = await req.ReadFormAsync();
            var file = form.Files.GetFile("file");

            if (file == null || file.Length == 0)
            {
                return new BadRequestObjectResult("画像ファイルが指定されていません。");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return new BadRequestObjectResult("サポートされていないファイル形式です。");
            }

            try
            {
                Stream streamToProcess = file.OpenReadStream();
                string fileNameToProcess = file.FileName;
                bool isPdf = extension == ".pdf";
                MemoryStream? pdfConvertedStream = null;

                // PDFの場合は画像に変換
                if (isPdf)
                {
                    var originalStream = streamToProcess;
                    
                    // 元のPDFを保存
                    originalStream.Position = 0;
                    await _imageStorageService.SaveImageAsync(originalStream, file.FileName, CancellationToken.None);
                    
                    // PDFを画像に変換
                    originalStream.Position = 0;
                    pdfConvertedStream = new MemoryStream();
                    
                    // PDFtoImageを使用して変換
                    await foreach (var image in Conversion.ToImagesAsync(originalStream))
                    {
                        image.Encode(pdfConvertedStream, SKEncodedImageFormat.Png, 100);
                        break; // 最初のページのみ
                    }
                    
                    pdfConvertedStream.Position = 0;
                    streamToProcess = pdfConvertedStream;
                    fileNameToProcess = Path.ChangeExtension(file.FileName, ".png");
                }

                // 処理用の画像を保存（非同期で並行実行）
                var saveTask = _imageStorageService.SaveImageAsync(streamToProcess, fileNameToProcess, CancellationToken.None);

                // ローカル一時ファイルに保存（OCR処理用）
                var tempFilePath = Path.Combine(Path.GetTempPath(), fileNameToProcess);
                streamToProcess.Position = 0;
                await using (var fileStream = File.Create(tempFilePath))
                {
                    await streamToProcess.CopyToAsync(fileStream, CancellationToken.None);
                }

                try
                {
                    // OCR処理
                    var ocrResult = await _ocrService.ExtractTextAsync(tempFilePath, CancellationToken.None);
                    
                    // 保存完了を待つ
                    var filePath = await saveTask;
                    
                    return new OkObjectResult(new { text = ocrResult, filePath });
                }
                finally
                {
                    // 一時ファイルをクリーンアップ
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                    
                    // PDFから変換した場合はストリームをクリーンアップ
                    pdfConvertedStream?.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR processing failed");
                return new ObjectResult(new { error = "OCR処理中にエラーが発生しました。", message = ex.Message }) { StatusCode = 500 };
            }
        }

        [Function("ProcessBatchImages")]
        public async Task<IActionResult> ProcessBatchImages([HttpTrigger(AuthorizationLevel.Function, "post", Route = "ocr/batch")] HttpRequest req)
        {
            if (!req.HasFormContentType)
            {
                return new BadRequestObjectResult("Content type must be multipart/form-data");
            }

            var form = await req.ReadFormAsync();
            var files = form.Files;

            if (files == null || files.Count == 0)
            {
                return new BadRequestObjectResult("画像ファイルが指定されていません。");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf" };
            var results = new List<object>();

            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                {
                    continue;
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    results.Add(new { fileName = file.FileName, error = "サポートされていない画像形式です。" });
                    continue;
                }

                try
                {
                    Stream streamToProcess = file.OpenReadStream();
                    string fileNameToProcess = file.FileName;
                    bool isPdf = extension == ".pdf";
                    MemoryStream? pdfConvertedStream = null;

                    // PDFの場合は画像に変換
                    if (isPdf)
                    {
                        var originalStream = streamToProcess;
                        
                        // 元のPDFを保存
                        originalStream.Position = 0;
                        await _imageStorageService.SaveImageAsync(originalStream, file.FileName, CancellationToken.None);
                        
                        // PDFを画像に変換
                        originalStream.Position = 0;
                        pdfConvertedStream = new MemoryStream();
                        
                        // PDFtoImageを使用して変換
                        await foreach (var image in Conversion.ToImagesAsync(originalStream))
                        {
                            image.Encode(pdfConvertedStream, SKEncodedImageFormat.Png, 100);
                            break; // 最初のページのみ
                        }
                        
                        pdfConvertedStream.Position = 0;
                        streamToProcess = pdfConvertedStream;
                        fileNameToProcess = Path.ChangeExtension(file.FileName, ".png");
                    }

                    // 処理用の画像を保存（非同期で並行実行）
                    var saveTask = _imageStorageService.SaveImageAsync(streamToProcess, fileNameToProcess, CancellationToken.None);

                    // ローカル一時ファイルに保存（OCR処理用）
                    var tempFilePath = Path.Combine(Path.GetTempPath(), fileNameToProcess);
                    streamToProcess.Position = 0;
                    await using (var fileStream = File.Create(tempFilePath))
                    {
                        await streamToProcess.CopyToAsync(fileStream, CancellationToken.None);
                    }

                    try
                    {
                        // OCR処理
                        var ocrResult = await _ocrService.ExtractTextAsync(tempFilePath, CancellationToken.None);
                        
                        // 保存完了を待つ
                        var filePath = await saveTask;
                        
                        results.Add(new { fileName = file.FileName, text = ocrResult, filePath });
                    }
                    finally
                    {
                        // 一時ファイルをクリーンアップ
                        if (File.Exists(tempFilePath))
                        {
                            File.Delete(tempFilePath);
                        }
                        
                        // PDFから変換した場合はストリームをクリーンアップ
                        pdfConvertedStream?.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"OCR processing failed for {file.FileName}");
                    results.Add(new { fileName = file.FileName, error = "OCR処理中にエラーが発生しました。", message = ex.Message });
                }
            }

            return new OkObjectResult(new { results });
        }
    }
}

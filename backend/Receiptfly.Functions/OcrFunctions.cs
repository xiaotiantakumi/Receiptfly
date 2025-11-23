using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Receiptfly.Application.Services;
using Azure.Storage.Blobs;

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
                // 画像を保存
                var filePath = await _imageStorageService.SaveImageAsync(file.OpenReadStream(), file.FileName, CancellationToken.None);

                // Azure Blob StorageのURIの場合、一時ファイルにダウンロード
                string localFilePath = filePath;
                bool isTemporaryFile = false;

                if (Uri.TryCreate(filePath, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
                {
                    // URIの場合、Azure SDKを使って一時ファイルにダウンロード
                    localFilePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(uri.LocalPath));
                    
                    var connectionString = _configuration.GetConnectionString("AzureStorage");
                    var blobClient = new BlobClient(connectionString, "receipt-images", Path.GetFileName(uri.LocalPath));
                    
                    await blobClient.DownloadToAsync(localFilePath, CancellationToken.None);
                    isTemporaryFile = true;
                }

                try
                {
                    // OCR処理
                    var ocrResult = await _ocrService.ExtractTextAsync(localFilePath, CancellationToken.None);
                    return new OkObjectResult(new { text = ocrResult, filePath });
                }
                finally
                {
                    // 一時ファイルをクリーンアップ
                    if (isTemporaryFile && File.Exists(localFilePath))
                    {
                        File.Delete(localFilePath);
                    }
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
                    // 画像を保存
                    var filePath = await _imageStorageService.SaveImageAsync(file.OpenReadStream(), file.FileName, CancellationToken.None);

                    // Azure Blob StorageのURIの場合、一時ファイルにダウンロード
                    string localFilePath = filePath;
                    bool isTemporaryFile = false;

                    if (Uri.TryCreate(filePath, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
                    {
                        // URIの場合、Azure SDKを使って一時ファイルにダウンロード
                        localFilePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(uri.LocalPath));
                        
                        var connectionString = _configuration.GetConnectionString("AzureStorage");
                        var blobClient = new BlobClient(connectionString, "receipt-images", Path.GetFileName(uri.LocalPath));
                        
                        await blobClient.DownloadToAsync(localFilePath, CancellationToken.None);
                        isTemporaryFile = true;
                    }

                    try
                    {
                        // OCR処理
                        var ocrResult = await _ocrService.ExtractTextAsync(localFilePath, CancellationToken.None);
                        results.Add(new { fileName = file.FileName, text = ocrResult, filePath });
                    }
                    finally
                    {
                        // 一時ファイルをクリーンアップ
                        if (isTemporaryFile && File.Exists(localFilePath))
                        {
                            File.Delete(localFilePath);
                        }
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

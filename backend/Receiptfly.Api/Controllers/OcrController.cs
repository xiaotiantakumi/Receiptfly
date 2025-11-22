using Microsoft.AspNetCore.Mvc;
using Receiptfly.Application.Services;
using Azure.Storage.Blobs;

namespace Receiptfly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OcrController : ControllerBase
{
    private readonly IImageStorageService _imageStorageService;
    private readonly IOcrService _ocrService;
    private readonly IConfiguration _configuration;

    public OcrController(IImageStorageService imageStorageService, IOcrService ocrService, IConfiguration configuration)
    {
        _imageStorageService = imageStorageService;
        _ocrService = ocrService;
        _configuration = configuration;
    }

    [HttpPost]
    public async Task<IActionResult> ProcessImage(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("画像ファイルが指定されていません。");
        }

        // ファイル形式の検証（画像とPDFをサポート）
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return BadRequest("サポートされていないファイル形式です。");
        }

        try
        {
            // 画像を保存
            var filePath = await _imageStorageService.SaveImageAsync(file.OpenReadStream(), file.FileName, cancellationToken);

            // Azure Blob StorageのURIの場合、一時ファイルにダウンロード
            string localFilePath = filePath;
            bool isTemporaryFile = false;

            if (Uri.TryCreate(filePath, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
            {
                // URIの場合、Azure SDKを使って一時ファイルにダウンロード
                localFilePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(uri.LocalPath));
                
                var connectionString = _configuration.GetConnectionString("AzureStorage");
                var blobClient = new BlobClient(connectionString, "receipt-images", Path.GetFileName(uri.LocalPath));
                
                await blobClient.DownloadToAsync(localFilePath, cancellationToken);
                isTemporaryFile = true;
            }

            try
            {
                // OCR処理
                var ocrResult = await _ocrService.ExtractTextAsync(localFilePath, cancellationToken);
                return Ok(new { text = ocrResult, filePath });
            }
            finally
            {
                // 一時ファイルをクリーンアップ
                if (isTemporaryFile && System.IO.File.Exists(localFilePath))
                {
                    System.IO.File.Delete(localFilePath);
                }
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "OCR処理中にエラーが発生しました。", message = ex.Message });
        }
    }

    [HttpPost("batch")]
    public async Task<IActionResult> ProcessBatchImages(List<IFormFile> files, CancellationToken cancellationToken)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest("画像ファイルが指定されていません。");
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
                var filePath = await _imageStorageService.SaveImageAsync(file.OpenReadStream(), file.FileName, cancellationToken);

                // Azure Blob StorageのURIの場合、一時ファイルにダウンロード
                string localFilePath = filePath;
                bool isTemporaryFile = false;

                if (Uri.TryCreate(filePath, UriKind.Absolute, out var uri) && (uri.Scheme == "http" || uri.Scheme == "https"))
                {
                    // URIの場合、Azure SDKを使って一時ファイルにダウンロード
                    localFilePath = Path.Combine(Path.GetTempPath(), Path.GetFileName(uri.LocalPath));
                    
                    var connectionString = _configuration.GetConnectionString("AzureStorage");
                    var blobClient = new BlobClient(connectionString, "receipt-images", Path.GetFileName(uri.LocalPath));
                    
                    await blobClient.DownloadToAsync(localFilePath, cancellationToken);
                    isTemporaryFile = true;
                }

                try
                {
                    // OCR処理
                    var ocrResult = await _ocrService.ExtractTextAsync(localFilePath, cancellationToken);
                    results.Add(new { fileName = file.FileName, text = ocrResult, filePath });
                }
                finally
                {
                    // 一時ファイルをクリーンアップ
                    if (isTemporaryFile && System.IO.File.Exists(localFilePath))
                    {
                        System.IO.File.Delete(localFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                results.Add(new { fileName = file.FileName, error = "OCR処理中にエラーが発生しました。", message = ex.Message });
            }
        }

        return Ok(new { results });
    }
}


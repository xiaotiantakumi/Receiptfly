using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Receiptfly.Application.Services;
using Receiptfly.Application.Commands.CreateReceipt;
using MediatR;
using Azure.Storage.Blobs;
using Azure.Storage.Queues.Models;
using System.Text.Json;
using PDFtoImage;
using SkiaSharp;

namespace Receiptfly.ProcessingFunc
{
    public class OcrProcessingFunctions
    {
        private readonly ILogger<OcrProcessingFunctions> _logger;
        private readonly IImageStorageService _imageStorageService;
        private readonly IOcrService _ocrService;
        private readonly IReceiptGenerationService _receiptGenerationService;
        private readonly IMediator _mediator;
        private readonly IConfiguration _configuration;
        private readonly BlobServiceClient _blobServiceClient;

        public OcrProcessingFunctions(
            ILogger<OcrProcessingFunctions> logger,
            IImageStorageService imageStorageService,
            IOcrService ocrService,
            IReceiptGenerationService receiptGenerationService,
            IMediator mediator,
            IConfiguration configuration)
        {
            _logger = logger;
            _imageStorageService = imageStorageService;
            _ocrService = ocrService;
            _receiptGenerationService = receiptGenerationService;
            _mediator = mediator;
            _configuration = configuration;
            
            var connectionString = configuration.GetConnectionString("AzureStorage") ?? "UseDevelopmentStorage=true";
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        [Function("ProcessOcrFromQueue")]
        public async Task ProcessOcrFromQueue(
            [QueueTrigger("ocr-processing-queue", Connection = "AzureWebJobsStorage")] string message,
            FunctionContext context)
        {
            _logger.LogInformation($"Processing OCR queue message: {message}");

            try
            {
                // Queue Triggerは自動的にBase64デコードしてくれるので、直接JSONとしてデシリアライズ
                // host.jsonでmessageEncodingがbase64に設定されている場合、Functionsは自動的にデコードする
                // そのため、messageパラメータは既にデコード済みのJSON文字列として受け取る
                var queueMessage = JsonSerializer.Deserialize<OcrQueueMessage>(message, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (queueMessage == null || string.IsNullOrEmpty(queueMessage.BlobPath))
                {
                    _logger.LogError("Invalid queue message: missing blobPath");
                    return;
                }

                _logger.LogInformation($"Processing blob: {queueMessage.BlobPath}");

                // 1. Blob Storageから画像を取得
                var blobPathParts = queueMessage.BlobPath.Split('/', 2);
                if (blobPathParts.Length != 2)
                {
                    _logger.LogError($"Invalid blob path format: {queueMessage.BlobPath}");
                    return;
                }

                var containerName = blobPathParts[0];
                var blobName = blobPathParts[1];

                var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                if (!await blobClient.ExistsAsync())
                {
                    _logger.LogError($"Blob not found: {queueMessage.BlobPath}");
                    return;
                }

                // 一時ファイルにダウンロード
                var tempFilePath = Path.Combine(Path.GetTempPath(), blobName);
                await blobClient.DownloadToAsync(tempFilePath);

                try
                {
                    // PDFの場合は画像に変換
                    string ocrInputPath = tempFilePath;
                    bool isPdf = Path.GetExtension(blobName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
                    
                    if (isPdf)
                    {
                        _logger.LogInformation($"Converting PDF to image: {blobName}");
                        var imagePath = Path.ChangeExtension(tempFilePath, ".png");
                        
                        using (var pdfStream = File.OpenRead(tempFilePath))
                        {
                            var imageStream = new MemoryStream();
                            await foreach (var image in Conversion.ToImagesAsync(pdfStream))
                            {
                                image.Encode(imageStream, SKEncodedImageFormat.Png, 100);
                                break; // 最初のページのみ
                            }
                            
                            imageStream.Position = 0;
                            await using (var fileStream = File.Create(imagePath))
                            {
                                await imageStream.CopyToAsync(fileStream);
                            }
                        }
                        
                        ocrInputPath = imagePath;
                        _logger.LogInformation($"PDF converted to image: {ocrInputPath}");
                    }

                    // 2. OCR処理
                    _logger.LogInformation($"Starting OCR processing for {blobName}");
                    var ocrResult = await _ocrService.ExtractTextAsync(ocrInputPath, CancellationToken.None);
                    _logger.LogInformation($"OCR completed. Text length: {ocrResult?.Length ?? 0}");
                    
                    // PDF変換で作成した一時画像ファイルを削除
                    if (isPdf && ocrInputPath != tempFilePath && File.Exists(ocrInputPath))
                    {
                        File.Delete(ocrInputPath);
                    }

                    if (string.IsNullOrWhiteSpace(ocrResult))
                    {
                        _logger.LogWarning($"OCR result is empty for {blobName}");
                        return;
                    }

                    // 3. レシート生成に必要な設定を取得
                    // デフォルト値を使用（フロントエンドから送られてくる場合はQueueメッセージに含める）
                    var accountTitles = queueMessage.AccountTitles ?? new List<string> { "消耗品費", "旅費交通費", "交際費", "福利厚生費", "会議費", "事務用品費", "雑費", "事業主貸", "未払金", "現金" };
                    var categories = queueMessage.Categories ?? new List<string> { "消耗品費", "旅費交通費", "交際費", "福利厚生費", "会議費", "事務用品費", "雑費", "食費", "被服費" };

                    // 4. レシート生成
                    _logger.LogInformation($"Generating receipt from OCR result");
                    var receiptData = await _receiptGenerationService.GenerateReceiptFromOcrAsync(
                        ocrResult,
                        accountTitles,
                        categories,
                        CancellationToken.None
                    );

                    // 5. レシート作成
                    var createCommand = new CreateReceiptCommand(
                        receiptData.Store,
                        receiptData.Date,
                        receiptData.Tel,
                        receiptData.PaymentMethod,
                        receiptData.Address,
                        receiptData.RegistrationNumber,
                        receiptData.CreditAccount,
                        receiptData.Items.Select(item => new CreateReceiptItemDto(
                            item.Name,
                            item.Amount,
                            item.IsTaxReturn,
                            item.Category,
                            item.AiCategory,
                            item.AiRisk,
                            item.Memo,
                            item.TaxType,
                            item.AccountTitle
                        )).ToList()
                    );

                    var receipt = await _mediator.Send(createCommand, CancellationToken.None);
                    _logger.LogInformation($"Receipt created successfully. ID: {receipt.Id}, Store: {receipt.Store}");
                }
                finally
                {
                    // 一時ファイルをクリーンアップ
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to process OCR from queue: {ex.Message}");
                throw; // Queueメッセージを再処理できるようにする
            }
        }
    }

    public class OcrQueueMessage
    {
        public string JobId { get; set; } = string.Empty;
        public string BlobPath { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public List<string>? AccountTitles { get; set; }
        public List<string>? Categories { get; set; }
    }
}


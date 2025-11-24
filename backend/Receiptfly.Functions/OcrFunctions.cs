using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using System.Text.Json;

namespace Receiptfly.Functions
{
    public class OcrFunctions
    {
        private readonly ILogger<OcrFunctions> _logger;
        private readonly IConfiguration _configuration;
        private readonly QueueServiceClient _queueServiceClient;

        public OcrFunctions(ILogger<OcrFunctions> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            
            var connectionString = configuration.GetConnectionString("AzureStorage") ?? "UseDevelopmentStorage=true";
            _queueServiceClient = new QueueServiceClient(connectionString);
        }


        [Function("QueueOcrProcessing")]
        public async Task<IActionResult> QueueOcrProcessing(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "queue-ocr")] HttpRequest req)
        {
            try
            {
                using var reader = new StreamReader(req.Body);
                var requestBody = await reader.ReadToEndAsync();
                var request = JsonSerializer.Deserialize<QueueOcrRequest>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (request == null || request.BlobPaths == null || request.BlobPaths.Count == 0)
                {
                    return new BadRequestObjectResult(new { error = "blobPaths is required" });
                }

                var queueClient = _queueServiceClient.GetQueueClient("ocr-processing-queue");
                await queueClient.CreateIfNotExistsAsync();

                var jobIds = new List<string>();
                var queued = 0;

                foreach (var blobPath in request.BlobPaths)
                {
                    try
                    {
                        var jobId = Guid.NewGuid().ToString();
                        var message = new OcrQueueMessage
                        {
                            JobId = jobId,
                            BlobPath = blobPath,
                            CreatedAt = DateTimeOffset.UtcNow,
                            AccountTitles = request.AccountTitles,
                            Categories = request.Categories
                        };

                        var messageJson = JsonSerializer.Serialize(message);
                        var messageBytes = System.Text.Encoding.UTF8.GetBytes(messageJson);
                        var base64Message = Convert.ToBase64String(messageBytes);

                        await queueClient.SendMessageAsync(base64Message);
                        jobIds.Add(jobId);
                        queued++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to queue OCR processing for blob: {blobPath}");
                    }
                }

                return new OkObjectResult(new
                {
                    queued = queued,
                    jobIds = jobIds
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue OCR processing");
                return new ObjectResult(new { error = "Failed to queue OCR processing", message = ex.Message })
                {
                    StatusCode = 500
                };
            }
        }
    }

    public class QueueOcrRequest
    {
        public List<string> BlobPaths { get; set; } = new();
        public List<string>? AccountTitles { get; set; }
        public List<string>? Categories { get; set; }
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

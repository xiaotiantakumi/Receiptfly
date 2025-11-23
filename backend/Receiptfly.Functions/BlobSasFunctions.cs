using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Receiptfly.Functions
{
    public class BlobSasFunctions
    {
        private readonly ILogger<BlobSasFunctions> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public BlobSasFunctions(ILogger<BlobSasFunctions> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
        }

        [Function("GetBlobSasToken")]
        public async Task<IActionResult> GetBlobSasToken(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getSas")] HttpRequest req)
        {
            try
            {
                var containerName = req.Query["containerName"].FirstOrDefault() ?? "receipt-images";
                var blobName = req.Query["blobName"].FirstOrDefault();

                // プロセッシングファンクションのURLを取得（環境変数またはデフォルト値）
                var processingFuncUrl = _configuration["ProcessingFunc:BaseUrl"] 
                    ?? Environment.GetEnvironmentVariable("PROCESSING_FUNC_URL") 
                    ?? "http://localhost:7072/api";

                // プロセッシングファンクションにリクエストを転送
                var queryParams = new List<string>();
                queryParams.Add($"containerName={Uri.EscapeDataString(containerName)}");
                if (!string.IsNullOrEmpty(blobName))
                {
                    queryParams.Add($"blobName={Uri.EscapeDataString(blobName)}");
                }

                var processingFuncRequestUrl = $"{processingFuncUrl}/getSas?{string.Join("&", queryParams)}";
                
                _logger.LogInformation($"Forwarding SAS token request to Processing Function: {processingFuncRequestUrl}");

                var response = await _httpClient.GetAsync(processingFuncRequestUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("SAS token successfully obtained from Processing Function");
                    return new ContentResult
                    {
                        Content = responseContent,
                        ContentType = "application/json",
                        StatusCode = (int)response.StatusCode
                    };
                }
                else
                {
                    _logger.LogError($"Processing Function returned error: {response.StatusCode} - {responseContent}");
                    return new ObjectResult(new { error = "Failed to generate SAS token", message = responseContent })
                    {
                        StatusCode = (int)response.StatusCode
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to forward SAS token request to Processing Function");
                return new ObjectResult(new { error = "Failed to generate SAS token", message = ex.Message })
                {
                    StatusCode = 500
                };
            }
        }
    }
}


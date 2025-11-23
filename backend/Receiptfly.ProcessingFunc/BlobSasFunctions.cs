using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Receiptfly.Application.Services;
using Azure.Storage.Sas;
using System.Net;
using Microsoft.AspNetCore.WebUtilities;

namespace Receiptfly.ProcessingFunc
{
    public class BlobSasFunctions
    {
        private readonly ILogger<BlobSasFunctions> _logger;
        private readonly IImageStorageService _imageStorageService;

        public BlobSasFunctions(ILogger<BlobSasFunctions> logger, IImageStorageService imageStorageService)
        {
            _logger = logger;
            _imageStorageService = imageStorageService;
        }

        [Function("GetBlobSasToken")]
        public async Task<HttpResponseData> GetBlobSasToken(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getSas")] HttpRequestData req,
            FunctionContext context)
        {
            var logger = context.GetLogger("GetBlobSasToken");
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            try
            {
                // クエリパラメータを取得
                var query = QueryHelpers.ParseQuery(req.Url.Query);
                var containerName = query.ContainsKey("containerName") && query["containerName"].Count > 0 
                    ? query["containerName"][0] 
                    : "receipt-images";
                var blobName = query.ContainsKey("blobName") && query["blobName"].Count > 0 
                    ? query["blobName"][0] 
                    : null;

                // AzureBlobImageStorageServiceにキャストしてSASトークンを生成
                if (_imageStorageService is Receiptfly.Infrastructure.Services.AzureBlobImageStorageService azureStorageService)
                {
                    var sasUrl = azureStorageService.GenerateSasToken(
                        containerName,
                        blobName,
                        BlobSasPermissions.Write | BlobSasPermissions.Create,
                        TimeSpan.FromHours(1)
                    );

                    var expiresOn = DateTimeOffset.UtcNow.AddHours(1);

                    var result = new
                    {
                        sasUrl = sasUrl,
                        containerName = containerName,
                        blobName = blobName,
                        expiresOn = expiresOn.ToString("O")
                    };

                    await response.WriteStringAsync(System.Text.Json.JsonSerializer.Serialize(result));
                    return response;
                }
                else
                {
                    logger.LogWarning("SAS token generation is only supported for Azure Blob Storage");
                    response = req.CreateResponse(HttpStatusCode.BadRequest);
                    response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                    await response.WriteStringAsync(System.Text.Json.JsonSerializer.Serialize(new { error = "SAS token generation is only supported for Azure Blob Storage" }));
                    return response;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate SAS token");
                response = req.CreateResponse(HttpStatusCode.InternalServerError);
                response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                await response.WriteStringAsync(System.Text.Json.JsonSerializer.Serialize(new { error = "Failed to generate SAS token", message = ex.Message }));
                return response;
            }
        }
    }
}


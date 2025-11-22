using Azure.Storage.Blobs;
using Receiptfly.Application.Services;

namespace Receiptfly.Infrastructure.Services;

public class AzureBlobImageStorageService : IImageStorageService
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobImageStorageService(string connectionString)
    {
        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient("receipt-images");
        _containerClient.CreateIfNotExists();
    }

    public async Task<string> SaveImageAsync(Stream imageStream, string fileName, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        await blobClient.UploadAsync(imageStream, overwrite: true, cancellationToken: cancellationToken);
        return blobClient.Uri.ToString();
    }

    public async Task<bool> DeleteImageAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        return await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }
}

using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Azure.Storage;
using Receiptfly.Application.Services;

namespace Receiptfly.Infrastructure.Services;

public class AzureBlobImageStorageService : IImageStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobContainerClient _containerClient;
    private readonly string _connectionString;

    public AzureBlobImageStorageService(string connectionString)
    {
        _connectionString = connectionString;
        _blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = _blobServiceClient.GetBlobContainerClient("receipt-images");
        _containerClient.CreateIfNotExists();
    }

    public async Task<string> SaveImageAsync(Stream imageStream, string fileName, CancellationToken cancellationToken = default)
    {
        return await SaveImageAsync(imageStream, fileName, null, cancellationToken);
    }

    public async Task<string> SaveImageAsync(Stream imageStream, string fileName, Dictionary<string, string>? metadata, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        
        if (metadata != null && metadata.Count > 0)
        {
            var options = new Azure.Storage.Blobs.Models.BlobUploadOptions
            {
                Metadata = metadata
            };
            await blobClient.UploadAsync(imageStream, options, cancellationToken);
        }
        else
        {
            await blobClient.UploadAsync(imageStream, overwrite: true, cancellationToken: cancellationToken);
        }
        
        return blobClient.Uri.ToString();
    }

    public async Task<bool> DeleteImageAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        return await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public string GenerateSasToken(string containerName, string? blobName = null, BlobSasPermissions permissions = BlobSasPermissions.Write | BlobSasPermissions.Create, TimeSpan? expiresOn = null)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        containerClient.CreateIfNotExists();

        var expires = expiresOn ?? TimeSpan.FromHours(1);
        
        // 開発環境（Azurite）ではHttp、本番環境ではHttpsを使用
        var isDevelopment = _connectionString.Contains("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase) ||
                           _connectionString.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                           _connectionString.Contains("localhost", StringComparison.OrdinalIgnoreCase);
        
        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = containerName,
            BlobName = blobName,
            Resource = blobName == null ? "c" : "b", // "c" for container, "b" for blob
            ExpiresOn = DateTimeOffset.UtcNow.Add(expires)
        };
        
        // ProtocolはSetPermissionsの後に設定する必要がある
        if (isDevelopment)
        {
            sasBuilder.Protocol = SasProtocol.None; // NoneはHttpとHttpsの両方を許可
        }
        else
        {
            sasBuilder.Protocol = SasProtocol.Https;
        }
        sasBuilder.SetPermissions(permissions);

        // Connection stringからSharedKeyCredentialを取得
        var accountName = ExtractAccountNameFromConnectionString(_connectionString);
        var accountKey = ExtractAccountKeyFromConnectionString(_connectionString);
        
        if (string.IsNullOrEmpty(accountName) || string.IsNullOrEmpty(accountKey))
        {
            throw new InvalidOperationException("Connection string must contain AccountName and AccountKey for SAS token generation");
        }

        var sharedKeyCredential = new StorageSharedKeyCredential(accountName, accountKey);
        var sasToken = sasBuilder.ToSasQueryParameters(sharedKeyCredential);
        
        if (blobName != null)
        {
            var blobClient = containerClient.GetBlobClient(blobName);
            return blobClient.Uri + "?" + sasToken;
        }
        else
        {
            return containerClient.Uri + "?" + sasToken;
        }
    }

    private string? ExtractAccountNameFromConnectionString(string connectionString)
    {
        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            if (part.StartsWith("AccountName=", StringComparison.OrdinalIgnoreCase))
            {
                return part.Substring("AccountName=".Length);
            }
        }
        // UseDevelopmentStorage=trueの場合はデフォルトのアカウント名を使用
        if (connectionString.Contains("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase))
        {
            return "devstoreaccount1";
        }
        return null;
    }

    private string? ExtractAccountKeyFromConnectionString(string connectionString)
    {
        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            if (part.StartsWith("AccountKey=", StringComparison.OrdinalIgnoreCase))
            {
                return part.Substring("AccountKey=".Length);
            }
        }
        // UseDevelopmentStorage=trueの場合はデフォルトのキーを使用
        if (connectionString.Contains("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase))
        {
            return "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";
        }
        return null;
    }
}

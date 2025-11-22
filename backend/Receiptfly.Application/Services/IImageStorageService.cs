namespace Receiptfly.Application.Services;

public interface IImageStorageService
{
    Task<string> SaveImageAsync(Stream imageStream, string fileName, CancellationToken cancellationToken = default);
    Task<bool> DeleteImageAsync(string filePath, CancellationToken cancellationToken = default);
}




namespace Receiptfly.Application.Services;

public interface IOcrService
{
    Task<string> ExtractTextAsync(string imageFilePath, CancellationToken cancellationToken = default);
}



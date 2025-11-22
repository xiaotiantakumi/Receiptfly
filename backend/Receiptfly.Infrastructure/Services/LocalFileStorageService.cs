using Receiptfly.Application.Services;

namespace Receiptfly.Infrastructure.Services;

public class LocalFileStorageService : IImageStorageService
{
    private readonly string _uploadsDirectory;

    public LocalFileStorageService(string? uploadsDirectory = null)
    {
        _uploadsDirectory = uploadsDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        
        // ディレクトリが存在しない場合は作成
        if (!Directory.Exists(_uploadsDirectory))
        {
            Directory.CreateDirectory(_uploadsDirectory);
        }
    }

    public async Task<string> SaveImageAsync(Stream imageStream, string fileName, CancellationToken cancellationToken = default)
    {
        // GUIDベースのファイル名を生成
        var extension = Path.GetExtension(fileName);
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(_uploadsDirectory, uniqueFileName);

        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await imageStream.CopyToAsync(fileStream, cancellationToken);
        }

        return filePath;
    }

    public Task<bool> DeleteImageAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}


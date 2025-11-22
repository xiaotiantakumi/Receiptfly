using Receiptfly.Infrastructure.Services;

namespace Receiptfly.Api.Tests.Services;

public class LocalFileStorageServiceTests
{
    [Fact]
    public async Task SaveImageAsync_ShouldSaveImageAndReturnFilePath()
    {
        // Arrange
        var testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDirectory);

        var service = new LocalFileStorageService(testDirectory);
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG header
        using var imageStream = new MemoryStream(testImageBytes);

        // Act
        var filePath = await service.SaveImageAsync(imageStream, "test.png");

        // Assert
        Assert.NotNull(filePath);
        Assert.True(File.Exists(filePath));
        Assert.Contains(testDirectory, filePath);

        // Cleanup
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        Directory.Delete(Path.Combine(testDirectory, "uploads"), true);
        Directory.Delete(testDirectory);
    }

    [Fact]
    public async Task DeleteImageAsync_ShouldDeleteExistingFile()
    {
        // Arrange
        var testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDirectory);

        var service = new LocalFileStorageService(testDirectory);
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        using var imageStream = new MemoryStream(testImageBytes);
        var filePath = await service.SaveImageAsync(imageStream, "test.png");

        // Act
        var result = await service.DeleteImageAsync(filePath);

        // Assert
        Assert.True(result);
        Assert.False(File.Exists(filePath));

        // Cleanup
        if (Directory.Exists(Path.Combine(testDirectory, "uploads")))
        {
            Directory.Delete(Path.Combine(testDirectory, "uploads"), true);
        }
        Directory.Delete(testDirectory);
    }

    [Fact]
    public async Task DeleteImageAsync_ShouldReturnFalseForNonExistentFile()
    {
        // Arrange
        var testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDirectory);

        var service = new LocalFileStorageService(testDirectory);
        var nonExistentPath = Path.Combine(testDirectory, "uploads", "nonexistent.png");

        // Act
        var result = await service.DeleteImageAsync(nonExistentPath);

        // Assert
        Assert.False(result);

        // Cleanup
        Directory.Delete(testDirectory, true);
    }
}


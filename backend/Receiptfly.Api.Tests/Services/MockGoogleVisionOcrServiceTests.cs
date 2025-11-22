using Receiptfly.Infrastructure.Services;

namespace Receiptfly.Api.Tests.Services;

public class MockGoogleVisionOcrServiceTests
{
    [Fact]
    public async Task ExtractTextAsync_ShouldReturnMockText()
    {
        // Arrange
        var service = new MockGoogleVisionOcrService();
        var testImagePath = Path.Combine(Path.GetTempPath(), "test.png");

        // Act
        var result = await service.ExtractTextAsync(testImagePath);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("スーパーライフ", result);
    }
}


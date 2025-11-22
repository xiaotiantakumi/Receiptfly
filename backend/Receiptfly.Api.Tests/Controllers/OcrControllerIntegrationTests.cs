using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Receiptfly.Application.Services;
using Receiptfly.Infrastructure.Services;

namespace Receiptfly.Api.Tests.Controllers;

public class OcrControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OcrControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // MockGoogleVisionOcrServiceを使用するように設定
                var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IOcrService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddScoped<IOcrService, MockGoogleVisionOcrService>();
                
                // テスト用のデータベースを使用
                var dbContextDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(Microsoft.EntityFrameworkCore.DbContextOptions<Receiptfly.Infrastructure.Data.ApplicationDbContext>));
                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }
                services.AddDbContext<Receiptfly.Infrastructure.Data.ApplicationDbContext>(options =>
                    options.UseSqlite("Data Source=receiptfly_test.db", b => b.MigrationsAssembly("Receiptfly.Infrastructure")));
            });
        });
    }

    [Fact]
    public async Task ProcessImage_ShouldReturnOcrResult()
    {
        // Arrange
        var client = _factory.CreateClient();
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        using var content = new MultipartFormDataContent();
        using var imageStream = new MemoryStream(testImageBytes);
        content.Add(new StreamContent(imageStream), "file", "test.png");

        // Act
        var response = await client.PostAsync("/api/ocr", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("text", responseContent);
        Assert.Contains("スーパーライフ", responseContent);
    }

    [Fact]
    public async Task ProcessBatchImages_ShouldReturnMultipleOcrResults()
    {
        // Arrange
        var client = _factory.CreateClient();
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        using var content = new MultipartFormDataContent();
        
        using var imageStream1 = new MemoryStream(testImageBytes);
        content.Add(new StreamContent(imageStream1), "files", "test1.png");
        
        using var imageStream2 = new MemoryStream(testImageBytes);
        content.Add(new StreamContent(imageStream2), "files", "test2.png");

        // Act
        var response = await client.PostAsync("/api/ocr/batch", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("results", responseContent);
        Assert.Contains("test1.png", responseContent);
        Assert.Contains("test2.png", responseContent);
    }
}


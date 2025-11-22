using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Receiptfly.Application.Services;
using Receiptfly.Infrastructure.Services;

namespace Receiptfly.Api.Tests.Controllers;

public class OcrControllerE2ETests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OcrControllerE2ETests(WebApplicationFactory<Program> factory)
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
    public async Task ProcessImage_E2E_ShouldCompleteFullFlow()
    {
        // Arrange
        var client = _factory.CreateClient();
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        using var content = new MultipartFormDataContent();
        using var imageStream = new MemoryStream(testImageBytes);
        content.Add(new StreamContent(imageStream), "file", "receipt.png");

        // Act - 画像アップロードからOCR結果取得まで
        var response = await client.PostAsync("/api/ocr", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        
        // OCR結果が含まれていることを確認
        Assert.Contains("text", responseContent);
        Assert.Contains("filePath", responseContent);
        Assert.Contains("スーパーライフ", responseContent);
        Assert.Contains("合計", responseContent);
    }

    [Fact]
    public async Task ProcessBatchImages_E2E_ShouldProcessMultipleImages()
    {
        // Arrange
        var client = _factory.CreateClient();
        var testImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        using var content = new MultipartFormDataContent();
        
        // 複数の画像を追加
        for (int i = 1; i <= 3; i++)
        {
            var imageStream = new MemoryStream(testImageBytes);
            content.Add(new StreamContent(imageStream), "files", $"receipt{i}.png");
        }

        // Act - 複数画像の一括アップロードとOCR処理
        var response = await client.PostAsync("/api/ocr/batch", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        
        // 全ての画像のOCR結果が含まれていることを確認
        Assert.Contains("results", responseContent);
        Assert.Contains("receipt1.png", responseContent);
        Assert.Contains("receipt2.png", responseContent);
        Assert.Contains("receipt3.png", responseContent);
    }
}


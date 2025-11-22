using Google.Api.Gax;
using Google.Cloud.Vision.V1;
using Receiptfly.Application.Services;
using System.Text;
using System.Text.Json;

namespace Receiptfly.Infrastructure.Services;

public class GoogleVisionOcrService : IOcrService
{
    private readonly ImageAnnotatorClient? _client;
    private readonly string? _apiKey;
    private readonly HttpClient _httpClient;

    public GoogleVisionOcrService(string? apiKey = null)
    {
        if (!string.IsNullOrEmpty(apiKey))
        {
            // APIキーを使用する場合（REST APIを直接呼び出す）
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _client = null;
        }
        else
        {
            // サービスアカウントキー（JSONファイル）を使用する場合
            _client = ImageAnnotatorClient.Create();
            _httpClient = new HttpClient();
        }
    }

    public async Task<string> ExtractTextAsync(string imageFilePath, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(_apiKey))
        {
            // APIキーを使用してREST APIを直接呼び出す
            return await ExtractTextWithApiKeyAsync(imageFilePath, cancellationToken);
        }
        else
        {
            // サービスアカウントキーを使用
            var image = Image.FromFile(imageFilePath);
            var response = await _client!.DetectTextAsync(image);

            if (response == null || !response.Any())
            {
                return string.Empty;
            }

            // 全てのテキストを結合
            return string.Join("\n", response.Select(annotation => annotation.Description));
        }
    }

    private async Task<string> ExtractTextWithApiKeyAsync(string imageFilePath, CancellationToken cancellationToken)
    {
        var imageBytes = await File.ReadAllBytesAsync(imageFilePath, cancellationToken);
        var base64Image = Convert.ToBase64String(imageBytes);

        var requestBody = new
        {
            requests = new[]
            {
                new
                {
                    image = new { content = base64Image },
                    features = new[] { new { type = "TEXT_DETECTION", maxResults = 10 } }
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var url = $"https://vision.googleapis.com/v1/images:annotate?key={_apiKey}";

        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"Google Cloud Vision API error: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

        if (result.TryGetProperty("responses", out var responses) && responses.GetArrayLength() > 0)
        {
            var firstResponse = responses[0];
            if (firstResponse.TryGetProperty("textAnnotations", out var textAnnotations) && textAnnotations.GetArrayLength() > 0)
            {
                var descriptions = textAnnotations.EnumerateArray()
                    .Select(annotation => annotation.GetProperty("description").GetString() ?? "")
                    .Where(s => !string.IsNullOrEmpty(s));
                return string.Join("\n", descriptions);
            }
        }

        return string.Empty;
    }
}


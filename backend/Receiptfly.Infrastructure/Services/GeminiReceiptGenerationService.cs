using Receiptfly.Application.Services;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.RepresentationModel;

namespace Receiptfly.Infrastructure.Services;

public class GeminiReceiptGenerationService : IReceiptGenerationService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelName;
    private readonly string _promptTemplate;

    public GeminiReceiptGenerationService(string apiKey, string? modelName = null)
    {
        _httpClient = new HttpClient();
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _modelName = modelName ?? "gemini-flash-lite-latest";
        
        // プロンプトテンプレートを読み込む
        var promptPath = FindPromptTemplatePath();
        
        if (!string.IsNullOrEmpty(promptPath) && File.Exists(promptPath))
        {
            _promptTemplate = LoadPromptTemplate(promptPath);
        }
        else
        {
            // デフォルトのプロンプトを使用
            _promptTemplate = GetDefaultPromptTemplate();
        }
    }

    public async Task<ReceiptGenerationResult> GenerateReceiptFromOcrAsync(
        string ocrText,
        List<string> accountTitles,
        List<string> categories,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ocrText))
        {
            throw new ArgumentException("OCR text cannot be empty", nameof(ocrText));
        }

        // プロンプトテンプレートからプロンプトを生成
        var prompt = _promptTemplate
            .Replace("{ocrText}", ocrText)
            .Replace("{accountTitles}", string.Join(", ", accountTitles))
            .Replace("{categories}", string.Join(", ", categories));

        // JSON Schemaを定義（構造化レスポンス用）
        var responseSchema = CreateResponseSchema();

        // リクエストボディを作成
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                responseSchema = responseSchema,
                responseMimeType = "application/json"
            },
            systemInstruction = new
            {
                parts = new[]
                {
                    new { text = "You are a helpful assistant that extracts receipt information from OCR text and returns structured JSON data." }
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelName}:generateContent?key={_apiKey}";

        var response = await _httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new Exception($"Gemini API error: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

        // レスポンスからテキストを取得
        if (result.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
        {
            var firstCandidate = candidates[0];
            if (firstCandidate.TryGetProperty("content", out var contentElement))
            {
                if (contentElement.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                {
                    var firstPart = parts[0];
                    if (firstPart.TryGetProperty("text", out var textElement))
                    {
                        var jsonText = textElement.GetString();
                        if (!string.IsNullOrEmpty(jsonText))
                        {
                            return ParseReceiptResult(jsonText);
                        }
                    }
                }
            }
        }

        throw new Exception("Failed to extract receipt data from Gemini API response");
    }

    private ReceiptGenerationResult ParseReceiptResult(string jsonText)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // JSONをパースして、プロパティ名をマッピング
        var jsonDoc = JsonDocument.Parse(jsonText);
        var root = jsonDoc.RootElement;

        var result = new ReceiptGenerationResult
        {
            Store = root.GetProperty("store").GetString() ?? throw new Exception("Store name is required"),
            Date = root.GetProperty("date").GetString() ?? throw new Exception("Date is required"),
            Total = root.TryGetProperty("total", out var totalProp) ? totalProp.GetInt32() : 0,
            Address = root.TryGetProperty("address", out var addressProp) ? addressProp.GetString() : null,
            Tel = root.TryGetProperty("tel", out var telProp) ? telProp.GetString() : null,
            PaymentMethod = root.TryGetProperty("paymentMethod", out var paymentMethodProp) ? paymentMethodProp.GetString() : null,
            RegistrationNumber = root.TryGetProperty("registrationNumber", out var regNumProp) ? regNumProp.GetString() : null,
            CreditAccount = root.TryGetProperty("creditAccount", out var creditAccountProp) ? creditAccountProp.GetString() : null,
            Items = new List<TransactionItemGenerationResult>()
        };

        if (root.TryGetProperty("items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var itemElement in itemsProp.EnumerateArray())
            {
                var item = new TransactionItemGenerationResult
                {
                    Name = itemElement.GetProperty("name").GetString() ?? throw new Exception("Item name is required"),
                    Amount = itemElement.GetProperty("amount").GetInt32(),
                    IsTaxReturn = itemElement.TryGetProperty("isTaxReturn", out var isTaxReturnProp) && isTaxReturnProp.GetBoolean(),
                    Category = itemElement.TryGetProperty("category", out var categoryProp) ? categoryProp.GetString() : null,
                    AiCategory = itemElement.TryGetProperty("aiCategory", out var aiCategoryProp) ? aiCategoryProp.GetString() : null,
                    AiRisk = itemElement.TryGetProperty("aiRisk", out var aiRiskProp) ? aiRiskProp.GetString() : null,
                    Memo = itemElement.TryGetProperty("memo", out var memoProp) ? memoProp.GetString() : null,
                    TaxType = itemElement.TryGetProperty("taxType", out var taxTypeProp) ? taxTypeProp.GetString() : null,
                    AccountTitle = itemElement.TryGetProperty("accountTitle", out var accountTitleProp) ? accountTitleProp.GetString() : null
                };
                result.Items.Add(item);
            }
        }

        if (result.Items.Count == 0)
        {
            throw new Exception("At least one item is required");
        }

        // Totalが0の場合はItemsの合計を計算
        if (result.Total == 0)
        {
            result.Total = result.Items.Sum(i => i.Amount);
        }

        return result;
    }

    private object CreateResponseSchema()
    {
        return new
        {
            type = "object",
            properties = new
            {
                store = new { type = "string", description = "店舗名" },
                date = new { type = "string", description = "日付" },
                total = new { type = "integer", description = "合計金額" },
                address = new { type = "string", description = "住所（任意）" },
                tel = new { type = "string", description = "電話番号（任意）" },
                paymentMethod = new { type = "string", description = "支払い方法（任意）" },
                registrationNumber = new { type = "string", description = "登録番号（任意）" },
                creditAccount = new { type = "string", description = "貸方科目（任意）" },
                items = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            name = new { type = "string", description = "商品名" },
                            amount = new { type = "integer", description = "金額" },
                            isTaxReturn = new { type = "boolean", description = "税込還元フラグ" },
                            category = new { type = "string", description = "カテゴリ（任意）" },
                            aiCategory = new { type = "string", description = "AIカテゴリ（任意）" },
                            aiRisk = new { type = "string", description = "AIリスク（Low, Medium, High）" },
                            memo = new { type = "string", description = "メモ（任意）" },
                            taxType = new { type = "string", description = "税率（10%, 8%, 0%）" },
                            accountTitle = new { type = "string", description = "勘定科目（任意）" }
                        },
                        required = new[] { "name", "amount" }
                    }
                }
            },
            required = new[] { "store", "date", "total", "items" }
        };
    }

    private string LoadPromptTemplate(string filePath)
    {
        try
        {
            var yaml = new YamlStream();
            using (var reader = new StringReader(File.ReadAllText(filePath)))
            {
                yaml.Load(reader);
            }

            var root = (YamlMappingNode)yaml.Documents[0].RootNode;
            if (root.Children.TryGetValue(new YamlScalarNode("prompt"), out var promptNode))
            {
                if (promptNode is YamlScalarNode scalarNode)
                {
                    return scalarNode.Value ?? GetDefaultPromptTemplate();
                }
                // 複数行文字列の場合
                return promptNode.ToString().TrimStart('|').Trim();
            }
        }
        catch (Exception ex)
        {
            // YAMLの読み込みに失敗した場合はデフォルトを使用
            Console.WriteLine($"Failed to load prompt template: {ex.Message}");
        }

        return GetDefaultPromptTemplate();
    }

    private string FindPromptTemplatePath()
    {
        var possiblePaths = new[]
        {
            // 実行時のベースディレクトリ
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Prompts", "receipt-generation-prompt.yml"),
            // アセンブリの場所
            Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", "Prompts", "receipt-generation-prompt.yml"),
            // 現在の作業ディレクトリ
            Path.Combine(Directory.GetCurrentDirectory(), "Prompts", "receipt-generation-prompt.yml"),
            // InfrastructureプロジェクトのPromptsフォルダ（開発時）
            Path.Combine(Directory.GetCurrentDirectory(), "..", "Receiptfly.Infrastructure", "Prompts", "receipt-generation-prompt.yml"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "Receiptfly.Infrastructure", "Prompts", "receipt-generation-prompt.yml"),
        };

        foreach (var path in possiblePaths)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                return path;
            }
        }

        return string.Empty;
    }

    private string GetDefaultPromptTemplate()
    {
        return @"あなたはレシートOCR結果から構造化されたレシートデータを生成するAIアシスタントです。

OCRで抽出されたテキストから、以下の情報を抽出してJSON形式で返してください：

- 店舗名（Store）
- 日付（Date）
- 合計金額（Total）
- 住所（Address、任意）
- 電話番号（Tel、任意）
- 支払い方法（PaymentMethod、任意）
- 登録番号（RegistrationNumber、任意）
- 貸方科目（CreditAccount、任意）
- 明細項目（Items）のリスト

各明細項目には以下を含めてください：
- 商品名（Name）
- 金額（Amount）
- 税込還元フラグ（IsTaxReturn）
- カテゴリ（Category、以下のリストから選択）
- AIカテゴリ（AiCategory、以下のリストから選択）
- AIリスク（AiRisk、""Low""、""Medium""、""High""のいずれか）
- メモ（Memo、任意）
- 税率（TaxType、""10%""、""8%""、""0%""のいずれか）
- 勘定科目（AccountTitle、以下のリストから選択）

利用可能な勘定科目リスト：
{accountTitles}

利用可能なカテゴリリスト：
{categories}

OCR結果テキスト：
{ocrText}

上記のOCR結果から情報を抽出し、指定された形式でJSONを返してください。
勘定科目とカテゴリは、提供されたリストの中から最も適切なものを選択してください。
リストにない値は使用しないでください。";
    }
}


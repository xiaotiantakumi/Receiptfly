using Microsoft.EntityFrameworkCore;
using Receiptfly.Application.Helpers;
using Receiptfly.Application.Interfaces;
using Receiptfly.Application.Services;
using Receiptfly.Infrastructure.Data;
using Receiptfly.Infrastructure.Services;
using Receiptfly.Infrastructure.Repositories;
using Receiptfly.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

// OCR Service - Google Cloud認証情報の設定（非同期処理のため先に設定）
var apiKey = builder.Configuration["GoogleCloud:ApiKey"] 
    ?? Environment.GetEnvironmentVariable("GOOGLE_CLOUD_API_KEY");
var credentialsPath = builder.Configuration["GoogleCloud:CredentialsPath"] 
    ?? Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

// APIキーがファイルパスの場合、ファイルから読み込む
if (!string.IsNullOrEmpty(apiKey) && File.Exists(apiKey))
{
    apiKey = File.ReadAllText(apiKey).Trim();
}

if (!string.IsNullOrEmpty(credentialsPath) && File.Exists(credentialsPath))
{
    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);
}

// Gemini API認証情報の設定
var geminiApiKeyFromConfig = builder.Configuration["Gemini:ApiKey"];
var geminiApiKeyFromEnv = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
var geminiApiKey = geminiApiKeyFromConfig ?? geminiApiKeyFromEnv;

Console.WriteLine($"[Gemini API Key] Config value: {(string.IsNullOrEmpty(geminiApiKeyFromConfig) ? "Not set" : geminiApiKeyFromConfig.Substring(0, Math.Min(3, geminiApiKeyFromConfig.Length)) + "...")}");
Console.WriteLine($"[Gemini API Key] Environment variable: {(string.IsNullOrEmpty(geminiApiKeyFromEnv) ? "Not set" : geminiApiKeyFromEnv.Substring(0, Math.Min(3, geminiApiKeyFromEnv.Length)) + "...")}");

var geminiModelName = builder.Configuration["Gemini:ModelName"] 
    ?? Environment.GetEnvironmentVariable("GEMINI_MODEL_NAME") 
    ?? "gemini-flash-lite-latest";

// Gemini APIキーがファイルパスの場合、ファイルから読み込む
if (!string.IsNullOrEmpty(geminiApiKey) && File.Exists(geminiApiKey))
{
    geminiApiKey = File.ReadAllText(geminiApiKey).Trim();
    Console.WriteLine($"[Gemini API Key] Loaded from file: {geminiApiKey.Substring(0, Math.Min(10, geminiApiKey.Length))}...");
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// MediatR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(Receiptfly.Application.Queries.GetReceipts.GetReceiptsQuery).Assembly);
});

// Current User Service
builder.Services.AddScoped<Receiptfly.Application.Interfaces.ICurrentUserService, Receiptfly.Application.Services.MockCurrentUserService>();

// Database Configuration
var useAzure = builder.Configuration.GetValue<bool>("UseAzure", false);
var azureConnectionString = builder.Configuration.GetConnectionString("AzureStorage") ?? "UseDevelopmentStorage=true";

if (useAzure)
{
    Console.WriteLine("[Database] Using Azure Table Storage & Blob Storage");
    
    // Register Azure Repositories
    builder.Services.AddScoped<IReceiptRepository>(provider => 
    {
        var currentUserService = provider.GetRequiredService<Receiptfly.Application.Interfaces.ICurrentUserService>();
        return new AzureTableReceiptRepository(azureConnectionString, currentUserService);
    });
        
    builder.Services.AddScoped<IImageStorageService>(provider => 
        new AzureBlobImageStorageService(azureConnectionString));
}
else
{
    Console.WriteLine("[Database] Using SQLite (EF Core)");
    
    // EF Core Configuration
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite("Data Source=receiptfly.db", b => b.MigrationsAssembly("Receiptfly.Infrastructure")));

    // Register EF Repositories
    builder.Services.AddScoped<IReceiptRepository, EfReceiptRepository>();
    
    // Local File Storage
    builder.Services.AddScoped<IImageStorageService>(provider =>
    {
        var environment = provider.GetRequiredService<IWebHostEnvironment>();
        var uploadsDirectory = Path.Combine(environment.ContentRootPath, "uploads");
        return new LocalFileStorageService(uploadsDirectory);
    });
}

// Register IApplicationDbContext (Only needed for EF Core path, but kept for compatibility if needed internally)
// If using Azure, this might throw if injected, but we refactored handlers to use IReceiptRepository.
if (!useAzure)
{
    builder.Services.AddScoped<IApplicationDbContext>(provider => 
        provider.GetRequiredService<ApplicationDbContext>());
}

// OCR Service
// 本番環境ではGoogleVisionOcrServiceを使用し、テスト環境ではMockGoogleVisionOcrServiceを使用
// appsettings.jsonで制御可能 (デフォルトはfalse)
var useMockOcr = builder.Configuration.GetValue<bool>("UseMockOcr", false);

if (useMockOcr)
{
    Console.WriteLine("[OCR Service] Using MockGoogleVisionOcrService (UseMockOcr=true)");
    builder.Services.AddScoped<IOcrService, MockGoogleVisionOcrService>();
}
else
{
    Console.WriteLine("[OCR Service] Using GoogleVisionOcrService");
    if (!string.IsNullOrEmpty(apiKey))
    {
        Console.WriteLine($"[OCR Service] Google Cloud API Key: {apiKey.Substring(0, Math.Min(3, apiKey.Length))}...");
    }
    // APIキーまたはサービスアカウントキーを使用
    builder.Services.AddScoped<IOcrService>(provider => new GoogleVisionOcrService(apiKey));
}

// Receipt Generation Service
// 開発環境でUseMockReceiptGenerationがtrueの場合はMockGeminiReceiptGenerationServiceを使用
// appsettings.jsonで制御可能 (デフォルトはfalse)
var useMockReceiptGeneration = builder.Configuration.GetValue<bool>("UseMockReceiptGeneration", false);

if (useMockReceiptGeneration)
{
    Console.WriteLine("[Receipt Generation Service] Using MockGeminiReceiptGenerationService (UseMockReceiptGeneration=true)");
    builder.Services.AddScoped<IReceiptGenerationService, MockGeminiReceiptGenerationService>();
}
else
{
    // Gemini APIキーが設定されている場合のみGeminiReceiptGenerationServiceを登録
    if (!string.IsNullOrEmpty(geminiApiKey) && geminiApiKey != "your-gemini-api-key-here")
    {
        Console.WriteLine($"[Receipt Generation Service] Using GeminiReceiptGenerationService (Model: {geminiModelName})");
        Console.WriteLine($"[Receipt Generation Service] Gemini API Key: {geminiApiKey.Substring(0, Math.Min(10, geminiApiKey.Length))}...");
        builder.Services.AddScoped<IReceiptGenerationService>(provider => 
            new GeminiReceiptGenerationService(geminiApiKey, geminiModelName));
    }
    else
    {
        Console.WriteLine("[Receipt Generation Service] Using MockGeminiReceiptGenerationService (Gemini API Key not configured)");
        if (string.IsNullOrEmpty(geminiApiKey))
        {
            Console.WriteLine("[Receipt Generation Service] Warning: Gemini API Key is not set. Please configure it in appsettings.Development.json or set GEMINI_API_KEY environment variable.");
        }
        else if (geminiApiKey == "your-gemini-api-key-here")
        {
            Console.WriteLine("[Receipt Generation Service] Warning: Gemini API Key is set to placeholder value. Please set actual API key.");
        }
        // APIキーが設定されていない場合はMockを使用
        builder.Services.AddScoped<IReceiptGenerationService, MockGeminiReceiptGenerationService>();
    }
}

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // In development, allow any origin (for ngrok, local dev, etc.)
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            // In production, restrict to specific origins
            policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// CORS must be before other middleware
app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

// Create uploads directory if it doesn't exist
var uploadsDirectory = Path.Combine(app.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploadsDirectory))
{
    Directory.CreateDirectory(uploadsDirectory);
}

// Apply Migrations & Seeding (Only for EF Core)
if (!useAzure)
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        // For now, use EnsureCreated to create the database schema
        // TODO: Fix migration discovery and use Migrate() instead
        db.Database.EnsureCreated();

        if (!db.Receipts.Any())
        {
            var userId = "user_default";
            var receipts = new List<Receipt>
            {
                new Receipt
                {
                    Id = IdGenerator.GenerateReceiptId(),
                    UserId = userId,
                    Store = "スーパーライフ",
                    Date = "2023年11月22日 10:23",
                    Total = 1340,
                    Address = "東京都渋谷区1-2-3",
                    Tel = "03-1234-5678",
                    PaymentMethod = "クレジットカード",
                    CreditAccount = "未払金",
                    RegistrationNumber = "T1234567890123",
                    Items = new List<TransactionItem>
                    {
                        new() { Id = IdGenerator.GenerateTransactionItemId(), ReceiptId = string.Empty, Name = "コピー用紙 A4", Amount = 450, IsTaxReturn = true, Category = "消耗品費", AiCategory = "消耗品費", AiRisk = "Low", Memo = "プリンター用", TaxType = "10%", AccountTitle = "消耗品費" },
                        new() { Id = IdGenerator.GenerateTransactionItemId(), ReceiptId = string.Empty, Name = "豚肉 300g", Amount = 890, IsTaxReturn = false, Category = "食費", AiCategory = "食費", AiRisk = "Low", TaxType = "8%", AccountTitle = "福利厚生費" }
                    }
                },
                new Receipt
                {
                    Id = IdGenerator.GenerateReceiptId(),
                    UserId = userId,
                    Store = "セブンイレブン",
                    Date = "2023年11月21日 18:45",
                    Total = 270,
                    Address = "東京都新宿区西新宿1-1-1",
                    Tel = "03-9876-5432",
                    PaymentMethod = "PayPay",
                    CreditAccount = "未払金",
                    RegistrationNumber = "T9876543210987",
                    Items = new List<TransactionItem>
                    {
                        new() { Id = IdGenerator.GenerateTransactionItemId(), ReceiptId = string.Empty, Name = "ボールペン", Amount = 120, IsTaxReturn = true, Category = "消耗品費", AiCategory = "事務用品費", AiRisk = "Low", Memo = "クライアント訪問用", TaxType = "10%", AccountTitle = "消耗品費" },
                        new() { Id = IdGenerator.GenerateTransactionItemId(), ReceiptId = string.Empty, Name = "おにぎり", Amount = 150, IsTaxReturn = false, Category = "食費", AiCategory = "食費", AiRisk = "Low", TaxType = "8%", AccountTitle = "会議費" }
                    }
                },
                new Receipt
                {
                    Id = IdGenerator.GenerateReceiptId(),
                    UserId = userId,
                    Store = "ユニクロ",
                    Date = "2023年11月20日 14:30",
                    Total = 3990,
                    Address = "東京都中央区銀座6-9-5",
                    Tel = "03-1111-2222",
                    PaymentMethod = "クレジットカード",
                    CreditAccount = "未払金",
                    RegistrationNumber = "T1122334455667",
                    Items = new List<TransactionItem>
                    {
                        new() { Id = IdGenerator.GenerateTransactionItemId(), ReceiptId = string.Empty, Name = "ヒートテッククルーネックT", Amount = 1290, IsTaxReturn = false, Category = "被服費", AiCategory = "被服費", AiRisk = "Medium", Memo = "私用？", TaxType = "10%", AccountTitle = "事業主貸" },
                        new() { Id = IdGenerator.GenerateTransactionItemId(), ReceiptId = string.Empty, Name = "ヒートテックタイツ", Amount = 1290, IsTaxReturn = false, Category = "被服費", AiCategory = "被服費", AiRisk = "Medium", TaxType = "10%", AccountTitle = "事業主貸" },
                        new() { Id = IdGenerator.GenerateTransactionItemId(), ReceiptId = string.Empty, Name = "3足組ソックス", Amount = 990, IsTaxReturn = false, Category = "被服費", AiCategory = "被服費", AiRisk = "Low", TaxType = "10%", AccountTitle = "事業主貸" },
                        new() { Id = IdGenerator.GenerateTransactionItemId(), ReceiptId = string.Empty, Name = "ショッピングバッグ", Amount = 420, IsTaxReturn = true, Category = "雑費", AiCategory = "消耗品費", AiRisk = "Low", Memo = "資料持ち運び用", TaxType = "10%", AccountTitle = "消耗品費" }
                    }
                },
                new Receipt
                {
                    Id = IdGenerator.GenerateReceiptId(),
                    UserId = userId,
                    Store = "タクシー（日本交通）",
                    Date = "2023年11月19日 23:15",
                    Total = 4500,
                    Address = "車内",
                    Tel = "03-3333-4444",
                    PaymentMethod = "GO Pay",
                    CreditAccount = "未払金",
                    RegistrationNumber = "T5566778899001",
                    Items = new List<TransactionItem>
                    {
                        new() { Id = IdGenerator.GenerateTransactionItemId(), ReceiptId = string.Empty, Name = "乗車料金", Amount = 4500, IsTaxReturn = true, Category = "旅費交通費", AiCategory = "旅費交通費", AiRisk = "Medium", Memo = "深夜帰宅（プロジェクト遅延対応）", TaxType = "10%", AccountTitle = "旅費交通費" }
                    }
                },
                new Receipt
                {
                    Id = IdGenerator.GenerateReceiptId(),
                    UserId = userId,
                    Store = "居酒屋 魚金",
                    Date = "2023年11月18日 20:00",
                    Total = 12000,
                    Address = "東京都港区新橋3-3-3",
                    Tel = "03-5555-6666",
                    PaymentMethod = "現金",
                    CreditAccount = "現金",
                    RegistrationNumber = "T9988776655443",
                    Items = new List<TransactionItem>
                    {
                        new() { Id = IdGenerator.GenerateTransactionItemId(), ReceiptId = string.Empty, Name = "飲食代", Amount = 12000, IsTaxReturn = true, Category = "交際費", AiCategory = "接待交際費", AiRisk = "High", Memo = "取引先（株式会社A）との会食", TaxType = "10%", AccountTitle = "接待交際費" }
                    }
                }
            };

            // Set ReceiptId for all items
            foreach (var receipt in receipts)
            {
                foreach (var item in receipt.Items)
                {
                    item.ReceiptId = receipt.Id;
                }
            }

            db.Receipts.AddRange(receipts);
            db.SaveChanges();
        }
    }
}

app.Run();

// Make Program class accessible for testing
public partial class Program { }

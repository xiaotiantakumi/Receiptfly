using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Receiptfly.Application.Interfaces;
using Receiptfly.Application.Services;
using Receiptfly.Infrastructure.Data;
using Receiptfly.Infrastructure.Services;
using Receiptfly.Infrastructure.Repositories;
using Receiptfly.Domain.Entities;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((context, config) =>
    {
        // local.settings.json is automatically loaded in development
        // We can add other sources if needed
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // HttpClient for forwarding requests to Processing Function
        services.AddHttpClient();

        // MediatR
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(typeof(Receiptfly.Application.Queries.GetReceipts.GetReceiptsQuery).Assembly);
        });

        // Database Configuration
        var useAzure = configuration.GetValue<bool>("UseAzure", false);
        var azureConnectionString = configuration.GetConnectionString("AzureStorage") ?? "UseDevelopmentStorage=true";

        if (useAzure)
        {
            // Register Azure Repositories
            services.AddScoped<IReceiptRepository>(provider => 
                new AzureTableReceiptRepository(azureConnectionString));
                
            services.AddScoped<IImageStorageService>(provider => 
                new AzureBlobImageStorageService(azureConnectionString));
        }
        else
        {
            // EF Core Configuration
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite("Data Source=receiptfly.db", b => b.MigrationsAssembly("Receiptfly.Infrastructure")));

            // Register EF Repositories
            services.AddScoped<IReceiptRepository, EfReceiptRepository>();
            
            // Local File Storage
            services.AddScoped<IImageStorageService>(provider =>
            {
                // In Functions, we might not have a persistent local storage in production,
                // but for local dev we can use a folder relative to the function app.
                // Note: In Azure, this will be read-only or ephemeral unless using Azure Files mount.
                // But since we switch to AzureBlobImageStorageService when UseAzure is true, this path is only for local dev.
                var hostingEnvironment = provider.GetRequiredService<IHostEnvironment>();
                var uploadsDirectory = Path.Combine(hostingEnvironment.ContentRootPath, "uploads");
                if (!Directory.Exists(uploadsDirectory))
                {
                    Directory.CreateDirectory(uploadsDirectory);
                }
                return new LocalFileStorageService(uploadsDirectory);
            });
        }

        // OCR Service
        var apiKey = configuration["GoogleCloud:ApiKey"] 
            ?? Environment.GetEnvironmentVariable("GOOGLE_CLOUD_API_KEY");
        var useMockOcr = configuration.GetValue<bool>("UseMockOcr", false);

        if (useMockOcr)
        {
            services.AddScoped<IOcrService, MockGoogleVisionOcrService>();
        }
        else
        {
            services.AddScoped<IOcrService>(provider => new GoogleVisionOcrService(apiKey));
        }

        // Receipt Generation Service
        var geminiApiKey = configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        var geminiModelName = configuration["Gemini:ModelName"] 
            ?? Environment.GetEnvironmentVariable("GEMINI_MODEL_NAME") 
            ?? "gemini-flash-lite-latest";
        var useMockReceiptGeneration = configuration.GetValue<bool>("UseMockReceiptGeneration", false);

        if (useMockReceiptGeneration)
        {
            services.AddScoped<IReceiptGenerationService, MockGeminiReceiptGenerationService>();
        }
        else
        {
             if (!string.IsNullOrEmpty(geminiApiKey) && geminiApiKey != "your-gemini-api-key-here")
            {
                services.AddScoped<IReceiptGenerationService>(provider => 
                    new GeminiReceiptGenerationService(geminiApiKey, geminiModelName));
            }
            else
            {
                services.AddScoped<IReceiptGenerationService, MockGeminiReceiptGenerationService>();
            }
        }
    })
    .Build();

// Database Initialization (for local development with SQLite)
using (var scope = host.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var configuration = services.GetRequiredService<IConfiguration>();
    var useAzure = configuration.GetValue<bool>("UseAzure", false);

    if (!useAzure)
    {
        try 
        {
            var db = services.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();

            if (!db.Receipts.Any())
            {
                var receipts = new List<Receipt>
                {
                    new Receipt
                    {
                        Id = Guid.NewGuid(),
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
                            new() { Id = Guid.NewGuid(), ReceiptId = Guid.Empty, Name = "コピー用紙 A4", Amount = 450, IsTaxReturn = true, Category = "消耗品費", AiCategory = "消耗品費", AiRisk = "Low", Memo = "プリンター用", TaxType = "10%", AccountTitle = "消耗品費" },
                            new() { Id = Guid.NewGuid(), ReceiptId = Guid.Empty, Name = "豚肉 300g", Amount = 890, IsTaxReturn = false, Category = "食費", AiCategory = "食費", AiRisk = "Low", TaxType = "8%", AccountTitle = "福利厚生費" }
                        }
                    },
                    new Receipt
                    {
                        Id = Guid.NewGuid(),
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
                            new() { Id = Guid.NewGuid(), ReceiptId = Guid.Empty, Name = "ボールペン", Amount = 120, IsTaxReturn = true, Category = "消耗品費", AiCategory = "事務用品費", AiRisk = "Low", Memo = "クライアント訪問用", TaxType = "10%", AccountTitle = "消耗品費" },
                            new() { Id = Guid.NewGuid(), ReceiptId = Guid.Empty, Name = "おにぎり", Amount = 150, IsTaxReturn = false, Category = "食費", AiCategory = "食費", AiRisk = "Low", TaxType = "8%", AccountTitle = "会議費" }
                        }
                    },
                    new Receipt
                    {
                        Id = Guid.NewGuid(),
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
                            new() { Id = Guid.NewGuid(), ReceiptId = Guid.Empty, Name = "ヒートテッククルーネックT", Amount = 1290, IsTaxReturn = false, Category = "被服費", AiCategory = "被服費", AiRisk = "Medium", Memo = "私用？", TaxType = "10%", AccountTitle = "事業主貸" },
                            new() { Id = Guid.NewGuid(), ReceiptId = Guid.Empty, Name = "ヒートテックタイツ", Amount = 1290, IsTaxReturn = false, Category = "被服費", AiCategory = "被服費", AiRisk = "Medium", TaxType = "10%", AccountTitle = "事業主貸" },
                            new() { Id = Guid.NewGuid(), ReceiptId = Guid.Empty, Name = "3足組ソックス", Amount = 990, IsTaxReturn = false, Category = "被服費", AiCategory = "被服費", AiRisk = "Low", TaxType = "10%", AccountTitle = "事業主貸" },
                            new() { Id = Guid.NewGuid(), ReceiptId = Guid.Empty, Name = "ショッピングバッグ", Amount = 420, IsTaxReturn = true, Category = "雑費", AiCategory = "消耗品費", AiRisk = "Low", Memo = "資料持ち運び用", TaxType = "10%", AccountTitle = "消耗品費" }
                        }
                    },
                    new Receipt
                    {
                        Id = Guid.NewGuid(),
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
                            new() { Id = Guid.NewGuid(), ReceiptId = Guid.Empty, Name = "乗車料金", Amount = 4500, IsTaxReturn = true, Category = "旅費交通費", AiCategory = "旅費交通費", AiRisk = "Medium", Memo = "深夜帰宅（プロジェクト遅延対応）", TaxType = "10%", AccountTitle = "旅費交通費" }
                        }
                    },
                    new Receipt
                    {
                        Id = Guid.NewGuid(),
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
                            new() { Id = Guid.NewGuid(), ReceiptId = Guid.Empty, Name = "飲食代", Amount = 12000, IsTaxReturn = true, Category = "交際費", AiCategory = "接待交際費", AiRisk = "High", Memo = "取引先（株式会社A）との会食", TaxType = "10%", AccountTitle = "接待交際費" }
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
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while initializing the database.");
        }
    }
}

host.Run();

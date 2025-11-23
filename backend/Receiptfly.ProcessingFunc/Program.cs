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
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

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
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while initializing the database.");
        }
    }
}

host.Run();

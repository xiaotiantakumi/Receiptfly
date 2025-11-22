using Microsoft.EntityFrameworkCore;
using Receiptfly.Api.Data;
using Receiptfly.Api.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=receiptfly.db"));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseHttpsRedirection();
app.MapControllers();

// Seed Data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Receipts.Any())
    {
        db.Receipts.AddRange(new[]
        {
            new Receipt
            {
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
                    new() { Name = "コピー用紙 A4", Amount = 450, IsTaxReturn = true, Category = "消耗品費", AiCategory = "消耗品費", AiRisk = "Low", Memo = "プリンター用", TaxType = "10%", AccountTitle = "消耗品費" },
                    new() { Name = "豚肉 300g", Amount = 890, IsTaxReturn = false, Category = "食費", AiCategory = "食費", AiRisk = "Low", TaxType = "8%", AccountTitle = "福利厚生費" }
                }
            },
            new Receipt
            {
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
                    new() { Name = "ボールペン", Amount = 120, IsTaxReturn = true, Category = "消耗品費", AiCategory = "事務用品費", AiRisk = "Low", Memo = "クライアント訪問用", TaxType = "10%", AccountTitle = "消耗品費" },
                    new() { Name = "おにぎり", Amount = 150, IsTaxReturn = false, Category = "食費", AiCategory = "食費", AiRisk = "Low", TaxType = "8%", AccountTitle = "会議費" }
                }
            },
            new Receipt
            {
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
                    new() { Name = "ヒートテッククルーネックT", Amount = 1290, IsTaxReturn = false, Category = "被服費", AiCategory = "被服費", AiRisk = "Medium", Memo = "私用？", TaxType = "10%", AccountTitle = "事業主貸" },
                    new() { Name = "ヒートテックタイツ", Amount = 1290, IsTaxReturn = false, Category = "被服費", AiCategory = "被服費", AiRisk = "Medium", TaxType = "10%", AccountTitle = "事業主貸" },
                    new() { Name = "3足組ソックス", Amount = 990, IsTaxReturn = false, Category = "被服費", AiCategory = "被服費", AiRisk = "Low", TaxType = "10%", AccountTitle = "事業主貸" },
                    new() { Name = "ショッピングバッグ", Amount = 420, IsTaxReturn = true, Category = "雑費", AiCategory = "消耗品費", AiRisk = "Low", Memo = "資料持ち運び用", TaxType = "10%", AccountTitle = "消耗品費" }
                }
            },
            new Receipt
            {
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
                    new() { Name = "乗車料金", Amount = 4500, IsTaxReturn = true, Category = "旅費交通費", AiCategory = "旅費交通費", AiRisk = "Medium", Memo = "深夜帰宅（プロジェクト遅延対応）", TaxType = "10%", AccountTitle = "旅費交通費" }
                }
            },
            new Receipt
            {
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
                    new() { Name = "飲食代", Amount = 12000, IsTaxReturn = true, Category = "交際費", AiCategory = "接待交際費", AiRisk = "High", Memo = "取引先（株式会社A）との会食", TaxType = "10%", AccountTitle = "接待交際費" }
                }
            }
        });
        db.SaveChanges();
    }
}

app.Run();

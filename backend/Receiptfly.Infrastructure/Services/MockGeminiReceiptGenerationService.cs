using Receiptfly.Application.Services;

namespace Receiptfly.Infrastructure.Services;

public class MockGeminiReceiptGenerationService : IReceiptGenerationService
{
    public Task<ReceiptGenerationResult> GenerateReceiptFromOcrAsync(
        string ocrText,
        List<string> accountTitles,
        List<string> categories,
        CancellationToken cancellationToken = default)
    {
        // モックデータを返す
        var result = new ReceiptGenerationResult
        {
            Store = "モック店舗",
            Date = DateTime.Now.ToString("yyyy年MM月dd日 HH:mm"),
            Total = 1500,
            Address = "東京都渋谷区1-2-3",
            Tel = "03-1234-5678",
            PaymentMethod = "現金",
            RegistrationNumber = "T1234567890123",
            CreditAccount = accountTitles.FirstOrDefault() ?? "現金",
            Items = new List<TransactionItemGenerationResult>
            {
                new TransactionItemGenerationResult
                {
                    Name = "モック商品1",
                    Amount = 800,
                    IsTaxReturn = true,
                    Category = categories.FirstOrDefault() ?? "消耗品費",
                    AiCategory = categories.FirstOrDefault() ?? "消耗品費",
                    AiRisk = "Low",
                    Memo = "テスト用データ",
                    TaxType = "10%",
                    AccountTitle = accountTitles.FirstOrDefault() ?? "消耗品費"
                },
                new TransactionItemGenerationResult
                {
                    Name = "モック商品2",
                    Amount = 700,
                    IsTaxReturn = false,
                    Category = categories.Skip(1).FirstOrDefault() ?? "食費",
                    AiCategory = categories.Skip(1).FirstOrDefault() ?? "食費",
                    AiRisk = "Low",
                    TaxType = "8%",
                    AccountTitle = accountTitles.Skip(1).FirstOrDefault() ?? "福利厚生費"
                }
            }
        };

        return Task.FromResult(result);
    }
}



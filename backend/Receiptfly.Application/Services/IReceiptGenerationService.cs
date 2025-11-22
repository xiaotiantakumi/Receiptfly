using Receiptfly.Domain.Entities;

namespace Receiptfly.Application.Services;

public interface IReceiptGenerationService
{
    Task<ReceiptGenerationResult> GenerateReceiptFromOcrAsync(
        string ocrText,
        List<string> accountTitles,
        List<string> categories,
        CancellationToken cancellationToken = default);
}

public class ReceiptGenerationResult
{
    public required string Store { get; set; }
    public required string Date { get; set; }
    public int Total { get; set; }
    public string? Address { get; set; }
    public string? Tel { get; set; }
    public string? PaymentMethod { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? CreditAccount { get; set; }
    public required List<TransactionItemGenerationResult> Items { get; set; }
}

public class TransactionItemGenerationResult
{
    public required string Name { get; set; }
    public int Amount { get; set; }
    public bool IsTaxReturn { get; set; }
    public string? Category { get; set; }
    public string? AiCategory { get; set; }
    public string? AiRisk { get; set; }
    public string? Memo { get; set; }
    public string? TaxType { get; set; }
    public string? AccountTitle { get; set; }
}



using System.ComponentModel.DataAnnotations;

namespace Receiptfly.Api.Models;

public class Receipt
{
    public int Id { get; set; }
    public string Store { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty; // Keeping as string for simplicity matching frontend mock
    public int Total { get; set; }
    public string? Address { get; set; }
    public string? Tel { get; set; }
    public string? PaymentMethod { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? CreditAccount { get; set; } // "現金", "未払金", etc.

    public List<TransactionItem> Items { get; set; } = new();
}

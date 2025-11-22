using System.Text.Json.Serialization;

namespace Receiptfly.Domain.Entities;

public class TransactionItem
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int Amount { get; set; }
    public bool IsTaxReturn { get; set; }
    public string? Category { get; set; }
    public string? AiCategory { get; set; }
    public string? AiRisk { get; set; }
    public string? Memo { get; set; }
    public string? TaxType { get; set; }
    public string? AccountTitle { get; set; }
    
    public int ReceiptId { get; set; }
    
    [JsonIgnore]
    public Receipt? Receipt { get; set; }
}

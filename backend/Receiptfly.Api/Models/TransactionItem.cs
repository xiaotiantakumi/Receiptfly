using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Receiptfly.Api.Models;

public class TransactionItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Amount { get; set; }
    public bool IsTaxReturn { get; set; }
    public string? Category { get; set; }
    public string? AiCategory { get; set; }
    public string? AiRisk { get; set; } // "Low", "Medium", "High"
    public string? Memo { get; set; }
    public string? TaxType { get; set; } // "10%", "8%", "0%"
    public string? AccountTitle { get; set; } // "消耗品費", "旅費交通費", etc.

    // Foreign Key
    public int ReceiptId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public Receipt? Receipt { get; set; }
}

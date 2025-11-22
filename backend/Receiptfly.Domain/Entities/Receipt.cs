namespace Receiptfly.Domain.Entities;

public class Receipt
{
    public Guid Id { get; set; }
    public required string Store { get; set; }
    public required string Date { get; set; }
    public int Total { get; set; }
    public string? Address { get; set; }
    public string? Tel { get; set; }
    public string? PaymentMethod { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? CreditAccount { get; set; }
    
    public ICollection<TransactionItem> Items { get; set; } = new List<TransactionItem>();

    /// <summary>
    /// Recalculates the total amount from all items
    /// </summary>
    public void RecalculateTotal()
    {
        Total = Items.Sum(item => item.Amount);
    }
}

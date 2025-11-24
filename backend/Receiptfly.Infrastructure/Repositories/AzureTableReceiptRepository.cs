using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Receiptfly.Application.Interfaces;
using Receiptfly.Domain.Entities;

namespace Receiptfly.Infrastructure.Repositories;

public class AzureTableReceiptRepository : IReceiptRepository
{
    private readonly TableClient _tableClient;
    private const string PartitionKey = "Receipt";

    public AzureTableReceiptRepository(string connectionString)
    {
        var serviceClient = new TableServiceClient(connectionString);
        _tableClient = serviceClient.GetTableClient("Receipts");
        _tableClient.CreateIfNotExists();
    }

    public async Task<Receipt?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {

        try
        {
            var response = await _tableClient.GetEntityAsync<ReceiptTableEntity>(PartitionKey, id.ToString(), cancellationToken: cancellationToken);
            return response.Value.ToReceipt();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<IEnumerable<Receipt>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var receipts = new List<Receipt>();
        var query = _tableClient.QueryAsync<ReceiptTableEntity>(filter: $"PartitionKey eq '{PartitionKey}'", cancellationToken: cancellationToken);

        await foreach (var entity in query)
        {
            receipts.Add(entity.ToReceipt());
        }

        return receipts.OrderByDescending(r => r.Date).ToList();
    }

    public async Task AddAsync(Receipt receipt, CancellationToken cancellationToken = default)
    {
        var entity = new ReceiptTableEntity(receipt);
        await _tableClient.AddEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(Receipt receipt, CancellationToken cancellationToken = default)
    {
        var entity = new ReceiptTableEntity(receipt);
        await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _tableClient.DeleteEntityAsync(PartitionKey, id.ToString(), cancellationToken: cancellationToken);
    }

    // Internal Entity for Table Storage
    public class ReceiptTableEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "Receipt";
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string Store { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Tel { get; set; }
        public string? PaymentMethod { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? CreditAccount { get; set; }
        public int Total { get; set; }
        
        // Serialized Items
        public string ItemsJson { get; set; } = "[]";

        public ReceiptTableEntity() { }

        public ReceiptTableEntity(Receipt receipt)
        {
            PartitionKey = "Receipt";
            RowKey = receipt.Id.ToString();
            Store = receipt.Store;
            Date = receipt.Date;
            Address = receipt.Address;
            Tel = receipt.Tel;
            PaymentMethod = receipt.PaymentMethod;
            RegistrationNumber = receipt.RegistrationNumber;
            CreditAccount = receipt.CreditAccount;
            Total = receipt.Total;
            ItemsJson = JsonSerializer.Serialize(receipt.Items);
        }

        public Receipt ToReceipt()
        {
            return new Receipt
            {
                Id = Guid.Parse(RowKey),
                Store = Store,
                Date = Date,
                Address = Address,
                Tel = Tel,
                PaymentMethod = PaymentMethod,
                RegistrationNumber = RegistrationNumber,
                CreditAccount = CreditAccount,
                Total = Total,
                Items = JsonSerializer.Deserialize<List<TransactionItem>>(ItemsJson) ?? new List<TransactionItem>()
            };
        }
    }
}


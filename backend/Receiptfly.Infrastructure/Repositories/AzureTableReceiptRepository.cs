using System.Text.Json;
using System.Text.RegularExpressions;
using Azure;
using Azure.Data.Tables;
using Receiptfly.Application.Interfaces;
using Receiptfly.Domain.Entities;

namespace Receiptfly.Infrastructure.Repositories;

public class AzureTableReceiptRepository : IReceiptRepository
{
    private readonly TableClient _tableClient;
    private readonly ICurrentUserService _currentUserService;

    public AzureTableReceiptRepository(string connectionString, ICurrentUserService currentUserService)
    {
        var serviceClient = new TableServiceClient(connectionString);
        _tableClient = serviceClient.GetTableClient("Receipts");
        _tableClient.CreateIfNotExists();
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Dateプロパティから年度を抽出（例: "2024年11月22日" → "2024"）
    /// </summary>
    private string ExtractYear(string date)
    {
        var match = Regex.Match(date, @"(\d{4})年");
        return match.Success ? match.Groups[1].Value : DateTime.Now.Year.ToString();
    }

    /// <summary>
    /// PartitionKeyを生成（{UserId}_{Year}形式）
    /// </summary>
    private string GeneratePartitionKey(string userId, string date)
    {
        var year = ExtractYear(date);
        return $"{userId}_{year}";
    }

    public async Task<Receipt?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            // PartitionKeyを特定するために、まずユーザーIDと年度を取得する必要がある
            // しかし、IDだけではPartitionKeyが分からないため、全パーティションを検索する必要がある
            // 効率化のため、RowKeyで検索（RowKeyはreceipt-{uuid}形式で一意）
            var userId = await _currentUserService.GetCurrentUserIdAsync(cancellationToken);
            
            // 年度ごとに検索（現在年から過去5年分を検索）
            var currentYear = DateTime.Now.Year;
            for (int year = currentYear; year >= currentYear - 5; year--)
            {
                var partitionKey = $"{userId}_{year}";
                try
                {
                    var response = await _tableClient.GetEntityAsync<ReceiptTableEntity>(partitionKey, id, cancellationToken: cancellationToken);
                    return response.Value.ToReceipt();
                }
                catch (RequestFailedException ex) when (ex.Status == 404)
                {
                    continue;
                }
            }
            
            return null;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<IEnumerable<Receipt>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var receipts = new List<Receipt>();
        var userId = await _currentUserService.GetCurrentUserIdAsync(cancellationToken);
        
        // 現在年から過去5年分のパーティションを検索
        var currentYear = DateTime.Now.Year;
        for (int year = currentYear; year >= currentYear - 5; year--)
        {
            var partitionKey = $"{userId}_{year}";
            var query = _tableClient.QueryAsync<ReceiptTableEntity>(filter: $"PartitionKey eq '{partitionKey}'", cancellationToken: cancellationToken);

            await foreach (var entity in query)
            {
                receipts.Add(entity.ToReceipt());
            }
        }

        return receipts.OrderByDescending(r => r.Date).ToList();
    }

    public async Task AddAsync(Receipt receipt, CancellationToken cancellationToken = default)
    {
        var partitionKey = GeneratePartitionKey(receipt.UserId, receipt.Date);
        var entity = new ReceiptTableEntity(receipt, partitionKey);
        await _tableClient.AddEntityAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(Receipt receipt, CancellationToken cancellationToken = default)
    {
        var partitionKey = GeneratePartitionKey(receipt.UserId, receipt.Date);
        var entity = new ReceiptTableEntity(receipt, partitionKey);
        await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        // GetByIdAsyncと同じロジックでPartitionKeyを特定
        var receipt = await GetByIdAsync(id, cancellationToken);
        if (receipt == null)
        {
            return;
        }

        var partitionKey = GeneratePartitionKey(receipt.UserId, receipt.Date);
        await _tableClient.DeleteEntityAsync(partitionKey, id, cancellationToken: cancellationToken);
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
        public string UserId { get; set; } = string.Empty;
        public string? OriginalFileName { get; set; }
        public int Total { get; set; }
        
        // Serialized Items
        public string ItemsJson { get; set; } = "[]";

        public ReceiptTableEntity() { }

        public ReceiptTableEntity(Receipt receipt, string partitionKey)
        {
            PartitionKey = partitionKey;
            RowKey = receipt.Id; // receipt-{uuid}形式をそのまま使用
            Store = receipt.Store;
            Date = receipt.Date;
            Address = receipt.Address;
            Tel = receipt.Tel;
            PaymentMethod = receipt.PaymentMethod;
            RegistrationNumber = receipt.RegistrationNumber;
            CreditAccount = receipt.CreditAccount;
            UserId = receipt.UserId;
            OriginalFileName = receipt.OriginalFileName;
            Total = receipt.Total;
            ItemsJson = JsonSerializer.Serialize(receipt.Items);
        }

        public Receipt ToReceipt()
        {
            return new Receipt
            {
                Id = RowKey, // receipt-{uuid}形式をそのまま使用
                UserId = UserId,
                Store = Store,
                Date = Date,
                Address = Address,
                Tel = Tel,
                PaymentMethod = PaymentMethod,
                RegistrationNumber = RegistrationNumber,
                CreditAccount = CreditAccount,
                OriginalFileName = OriginalFileName,
                Total = Total,
                Items = JsonSerializer.Deserialize<List<TransactionItem>>(ItemsJson) ?? new List<TransactionItem>()
            };
        }
    }
}


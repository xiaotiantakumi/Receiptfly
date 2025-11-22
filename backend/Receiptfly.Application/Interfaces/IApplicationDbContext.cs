using Microsoft.EntityFrameworkCore;
using Receiptfly.Domain.Entities;

namespace Receiptfly.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Receipt> Receipts { get; }
    DbSet<TransactionItem> TransactionItems { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

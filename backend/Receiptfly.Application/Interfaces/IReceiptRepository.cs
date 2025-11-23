using Receiptfly.Domain.Entities;

namespace Receiptfly.Application.Interfaces;

public interface IReceiptRepository
{
    Task<Receipt?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Receipt>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Receipt receipt, CancellationToken cancellationToken = default);
    Task UpdateAsync(Receipt receipt, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}

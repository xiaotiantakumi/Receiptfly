using Microsoft.EntityFrameworkCore;
using Receiptfly.Application.Interfaces;
using Receiptfly.Domain.Entities;
using Receiptfly.Infrastructure.Data;

namespace Receiptfly.Infrastructure.Repositories;

public class EfReceiptRepository : IReceiptRepository
{
    private readonly ApplicationDbContext _context;

    public EfReceiptRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Receipt?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.Receipts
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Receipt>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Receipts
            .Include(r => r.Items)
            .OrderByDescending(r => r.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Receipt receipt, CancellationToken cancellationToken = default)
    {
        _context.Receipts.Add(receipt);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Receipt receipt, CancellationToken cancellationToken = default)
    {
        _context.Entry(receipt).State = EntityState.Modified;
        // Items might need handling if they are modified/added/deleted, 
        // but for now assuming the receipt object graph is correctly tracked or we rely on EF Core's tracking if fetched from context.
        // If it's a disconnected entity, we might need more complex update logic, but for this adapter, simple update is a good start.
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var receipt = await _context.Receipts.FindAsync(new object[] { id }, cancellationToken);
        if (receipt != null)
        {
            _context.Receipts.Remove(receipt);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

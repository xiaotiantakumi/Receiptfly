using MediatR;
using Microsoft.EntityFrameworkCore;
using Receiptfly.Application.Interfaces;
using Receiptfly.Domain.Entities;

namespace Receiptfly.Application.Queries.GetReceipts;

public class GetReceiptsQueryHandler : IRequestHandler<GetReceiptsQuery, List<Receipt>>
{
    private readonly IApplicationDbContext _context;

    public GetReceiptsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Receipt>> Handle(GetReceiptsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Receipts
            .Include(r => r.Items)
            .ToListAsync(cancellationToken);
    }
}

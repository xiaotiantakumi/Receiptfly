using MediatR;
using Microsoft.EntityFrameworkCore;
using Receiptfly.Application.Interfaces;
using Receiptfly.Domain.Entities;

namespace Receiptfly.Application.Queries.GetReceiptById;

public class GetReceiptByIdQueryHandler : IRequestHandler<GetReceiptByIdQuery, Receipt?>
{
    private readonly IApplicationDbContext _context;

    public GetReceiptByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Receipt?> Handle(GetReceiptByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.Receipts
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);
    }
}

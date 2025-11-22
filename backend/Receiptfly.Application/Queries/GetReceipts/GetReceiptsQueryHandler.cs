using MediatR;
using Microsoft.EntityFrameworkCore;
using Receiptfly.Application.Interfaces;
using Receiptfly.Domain.Entities;

namespace Receiptfly.Application.Queries.GetReceipts;

public class GetReceiptsQueryHandler : IRequestHandler<GetReceiptsQuery, List<Receipt>>
{
    private readonly IReceiptRepository _repository;

    public GetReceiptsQueryHandler(IReceiptRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<Receipt>> Handle(GetReceiptsQuery request, CancellationToken cancellationToken)
    {
        var receipts = await _repository.GetAllAsync(cancellationToken);
        return receipts.ToList();
    }
}

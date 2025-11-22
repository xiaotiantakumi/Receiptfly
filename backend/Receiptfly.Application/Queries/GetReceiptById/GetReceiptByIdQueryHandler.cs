using MediatR;
using Microsoft.EntityFrameworkCore;
using Receiptfly.Application.Interfaces;
using Receiptfly.Domain.Entities;

namespace Receiptfly.Application.Queries.GetReceiptById;

public class GetReceiptByIdQueryHandler : IRequestHandler<GetReceiptByIdQuery, Receipt?>
{
    private readonly IReceiptRepository _repository;

    public GetReceiptByIdQueryHandler(IReceiptRepository repository)
    {
        _repository = repository;
    }

    public async Task<Receipt?> Handle(GetReceiptByIdQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByIdAsync(request.Id, cancellationToken);
    }
}

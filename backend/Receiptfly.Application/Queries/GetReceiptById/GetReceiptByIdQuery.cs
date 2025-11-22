using MediatR;
using Receiptfly.Domain.Entities;

namespace Receiptfly.Application.Queries.GetReceiptById;

public record GetReceiptByIdQuery(Guid Id) : IRequest<Receipt?>;

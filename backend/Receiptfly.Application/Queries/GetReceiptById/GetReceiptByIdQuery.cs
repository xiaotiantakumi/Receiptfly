using MediatR;
using Receiptfly.Domain.Entities;

namespace Receiptfly.Application.Queries.GetReceiptById;

public record GetReceiptByIdQuery(string Id) : IRequest<Receipt?>;

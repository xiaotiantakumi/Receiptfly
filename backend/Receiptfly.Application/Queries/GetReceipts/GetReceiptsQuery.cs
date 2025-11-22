using MediatR;
using Receiptfly.Domain.Entities;

namespace Receiptfly.Application.Queries.GetReceipts;

public record GetReceiptsQuery : IRequest<List<Receipt>>;

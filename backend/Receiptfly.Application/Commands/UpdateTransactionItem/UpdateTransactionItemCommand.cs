using MediatR;

namespace Receiptfly.Application.Commands.UpdateTransactionItem;

public record UpdateTransactionItemCommand(
    string ReceiptId,
    string ItemId,
    string? Name,
    int? Amount,
    bool? IsTaxReturn,
    string? Category,
    string? AiCategory,
    string? AiRisk,
    string? Memo,
    string? TaxType,
    string? AccountTitle
) : IRequest<bool>;

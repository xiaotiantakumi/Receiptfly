using MediatR;

namespace Receiptfly.Application.Commands.UpdateTransactionItem;

public record UpdateTransactionItemCommand(
    int ReceiptId,
    int ItemId,
    bool? IsTaxReturn,
    string? Category,
    string? AiCategory,
    string? AiRisk,
    string? Memo,
    string? TaxType,
    string? AccountTitle
) : IRequest<bool>;

using MediatR;
using Receiptfly.Domain.Entities;

namespace Receiptfly.Application.Commands.CreateReceipt;

public record CreateReceiptCommand(
    string Store,
    string Date,
    string? Tel,
    string? PaymentMethod,
    string? Address,
    string? RegistrationNumber,
    string? CreditAccount,
    List<CreateReceiptItemDto> Items,
    string? OriginalFileName = null
) : IRequest<Receipt>;

public record CreateReceiptItemDto(
    string Name,
    int Amount,
    bool? IsTaxReturn,
    string? Category,
    string? AiCategory,
    string? AiRisk,
    string? Memo,
    string? TaxType,
    string? AccountTitle
);

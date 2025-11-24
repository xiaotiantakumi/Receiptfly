using MediatR;

namespace Receiptfly.Application.Commands.UpdateReceipt;

public record UpdateReceiptCommand(
    Guid Id,
    string? Store,
    string? Date,
    string? Address,
    string? Tel,
    string? PaymentMethod,
    string? RegistrationNumber,
    string? CreditAccount
) : IRequest<bool>;

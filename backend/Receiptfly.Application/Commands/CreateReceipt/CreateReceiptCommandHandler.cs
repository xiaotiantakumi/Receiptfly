using MediatR;
using Receiptfly.Application.Helpers;
using Receiptfly.Application.Interfaces;
using Receiptfly.Domain.Entities;

namespace Receiptfly.Application.Commands.CreateReceipt;

public class CreateReceiptCommandHandler : IRequestHandler<CreateReceiptCommand, Receipt>
{
    private readonly IReceiptRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public CreateReceiptCommandHandler(IReceiptRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public async Task<Receipt> Handle(CreateReceiptCommand request, CancellationToken cancellationToken)
    {
        var receiptId = IdGenerator.GenerateReceiptId();
        var userId = await _currentUserService.GetCurrentUserIdAsync(cancellationToken);
        
        var receipt = new Receipt
        {
            Id = receiptId,
            UserId = userId,
            Store = request.Store,
            Date = request.Date,
            Address = request.Address,
            Tel = request.Tel,
            PaymentMethod = request.PaymentMethod,
            RegistrationNumber = request.RegistrationNumber,
            CreditAccount = request.CreditAccount,
            OriginalFileName = request.OriginalFileName,
            Items = request.Items.Select(item => new TransactionItem
            {
                Id = IdGenerator.GenerateTransactionItemId(),
                ReceiptId = receiptId,
                Name = item.Name,
                Amount = item.Amount,
                IsTaxReturn = item.IsTaxReturn ?? false,
                Category = item.Category,
                AiCategory = item.AiCategory,
                AiRisk = item.AiRisk ?? "Low",
                Memo = item.Memo,
                TaxType = item.TaxType,
                AccountTitle = item.AccountTitle
            }).ToList()
        };

        receipt.RecalculateTotal();

        await _repository.AddAsync(receipt, cancellationToken);

        return receipt;
    }
}

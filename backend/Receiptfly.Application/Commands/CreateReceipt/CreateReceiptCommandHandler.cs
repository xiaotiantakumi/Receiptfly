using MediatR;
using Receiptfly.Application.Interfaces;
using Receiptfly.Domain.Entities;

namespace Receiptfly.Application.Commands.CreateReceipt;

public class CreateReceiptCommandHandler : IRequestHandler<CreateReceiptCommand, Receipt>
{
    private readonly IReceiptRepository _repository;

    public CreateReceiptCommandHandler(IReceiptRepository repository)
    {
        _repository = repository;
    }

    public async Task<Receipt> Handle(CreateReceiptCommand request, CancellationToken cancellationToken)
    {
        var receiptId = Guid.NewGuid();
        var receipt = new Receipt
        {
            Id = receiptId,
            Store = request.Store,
            Date = request.Date,
            Address = request.Address,
            Tel = request.Tel,
            PaymentMethod = request.PaymentMethod,
            RegistrationNumber = request.RegistrationNumber,
            CreditAccount = request.CreditAccount,
            Items = request.Items.Select(item => new TransactionItem
            {
                Id = Guid.NewGuid(),
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

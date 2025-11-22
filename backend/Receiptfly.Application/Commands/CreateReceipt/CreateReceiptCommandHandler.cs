using MediatR;
using Receiptfly.Application.Interfaces;
using Receiptfly.Domain.Entities;

namespace Receiptfly.Application.Commands.CreateReceipt;

public class CreateReceiptCommandHandler : IRequestHandler<CreateReceiptCommand, Receipt>
{
    private readonly IApplicationDbContext _context;

    public CreateReceiptCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Receipt> Handle(CreateReceiptCommand request, CancellationToken cancellationToken)
    {
        var receipt = new Receipt
        {
            Store = request.Store,
            Date = request.Date,
            Address = request.Address,
            Tel = request.Tel,
            PaymentMethod = request.PaymentMethod,
            RegistrationNumber = request.RegistrationNumber,
            CreditAccount = request.CreditAccount,
            Items = request.Items.Select(item => new TransactionItem
            {
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

        receipt.Total = receipt.Items.Sum(i => i.Amount);

        _context.Receipts.Add(receipt);
        await _context.SaveChangesAsync(cancellationToken);

        return receipt;
    }
}

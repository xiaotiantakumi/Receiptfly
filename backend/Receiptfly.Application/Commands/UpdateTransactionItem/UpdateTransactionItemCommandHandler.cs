using MediatR;
using Receiptfly.Application.Interfaces;

namespace Receiptfly.Application.Commands.UpdateTransactionItem;

public class UpdateTransactionItemCommandHandler : IRequestHandler<UpdateTransactionItemCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateTransactionItemCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateTransactionItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.TransactionItems.FindAsync(new object[] { request.ItemId }, cancellationToken);

        if (item == null || item.ReceiptId != request.ReceiptId)
        {
            return false;
        }

        if (request.IsTaxReturn.HasValue) item.IsTaxReturn = request.IsTaxReturn.Value;
        if (request.Category != null) item.Category = request.Category;
        if (request.AiCategory != null) item.AiCategory = request.AiCategory;
        if (request.AiRisk != null) item.AiRisk = request.AiRisk;
        if (request.Memo != null) item.Memo = request.Memo;
        if (request.TaxType != null) item.TaxType = request.TaxType;
        if (request.AccountTitle != null) item.AccountTitle = request.AccountTitle;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}

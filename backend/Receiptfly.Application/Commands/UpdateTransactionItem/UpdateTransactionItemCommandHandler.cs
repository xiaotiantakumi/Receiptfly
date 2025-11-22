using MediatR;
using Receiptfly.Application.Interfaces;

namespace Receiptfly.Application.Commands.UpdateTransactionItem;

public class UpdateTransactionItemCommandHandler : IRequestHandler<UpdateTransactionItemCommand, bool>
{
    private readonly IReceiptRepository _repository;

    public UpdateTransactionItemCommandHandler(IReceiptRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(UpdateTransactionItemCommand request, CancellationToken cancellationToken)
    {
        var receipt = await _repository.GetByIdAsync(request.ReceiptId, cancellationToken);

        if (receipt == null)
        {
            return false;
        }

        var item = receipt.Items.FirstOrDefault(i => i.Id == request.ItemId);

        if (item == null)
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

        await _repository.UpdateAsync(receipt, cancellationToken);

        return true;
    }
}

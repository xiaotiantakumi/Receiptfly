using MediatR;
using Receiptfly.Application.Interfaces;

namespace Receiptfly.Application.Commands.UpdateReceipt;

public class UpdateReceiptCommandHandler : IRequestHandler<UpdateReceiptCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateReceiptCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UpdateReceiptCommand request, CancellationToken cancellationToken)
    {
        var receipt = await _context.Receipts.FindAsync(new object[] { request.Id }, cancellationToken);

        if (receipt == null)
        {
            return false;
        }

        if (request.Store != null) receipt.Store = request.Store;
        if (request.Date != null) receipt.Date = request.Date;
        if (request.Address != null) receipt.Address = request.Address;
        if (request.Tel != null) receipt.Tel = request.Tel;
        if (request.PaymentMethod != null) receipt.PaymentMethod = request.PaymentMethod;
        if (request.RegistrationNumber != null) receipt.RegistrationNumber = request.RegistrationNumber;
        if (request.CreditAccount != null) receipt.CreditAccount = request.CreditAccount;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}

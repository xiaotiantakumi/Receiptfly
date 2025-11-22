using MediatR;
using Microsoft.AspNetCore.Mvc;
using Receiptfly.Application.Commands.CreateReceipt;
using Receiptfly.Application.Commands.UpdateReceipt;
using Receiptfly.Application.Commands.UpdateTransactionItem;
using Receiptfly.Application.Queries.GetReceiptById;
using Receiptfly.Application.Queries.GetReceipts;

namespace Receiptfly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReceiptsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReceiptsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetReceipts()
    {
        var receipts = await _mediator.Send(new GetReceiptsQuery());
        return Ok(receipts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetReceipt(int id)
    {
        var receipt = await _mediator.Send(new GetReceiptByIdQuery(id));

        if (receipt == null)
        {
            return NotFound();
        }

        return Ok(receipt);
    }

    [HttpPut("{id}/items/{itemId}")]
    public async Task<IActionResult> UpdateItem(int id, int itemId, [FromBody] UpdateItemRequest request)
    {
        var command = new UpdateTransactionItemCommand(
            id,
            itemId,
            request.IsTaxReturn,
            request.Category,
            request.AiCategory,
            request.AiRisk,
            request.Memo,
            request.TaxType,
            request.AccountTitle
        );

        var result = await _mediator.Send(command);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateReceipt(int id, [FromBody] UpdateReceiptRequest request)
    {
        var command = new UpdateReceiptCommand(
            id,
            request.Store,
            request.Date,
            request.Address,
            request.Tel,
            request.PaymentMethod,
            request.RegistrationNumber,
            request.CreditAccount
        );

        var result = await _mediator.Send(command);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> CreateReceipt([FromBody] CreateReceiptRequest request)
    {
        var command = new CreateReceiptCommand(
            request.Store,
            request.Date,
            request.Tel,
            request.PaymentMethod,
            request.Address,
            request.RegistrationNumber,
            request.CreditAccount,
            request.Items.Select(item => new CreateReceiptItemDto(
                item.Name,
                item.Amount,
                item.IsTaxReturn,
                item.Category,
                item.AiCategory,
                item.AiRisk,
                item.Memo,
                item.TaxType,
                item.AccountTitle
            )).ToList()
        );

        var receipt = await _mediator.Send(command);

        return CreatedAtAction(nameof(GetReceipt), new { id = receipt.Id }, receipt);
    }

    public class UpdateItemRequest
    {
        public bool? IsTaxReturn { get; set; }
        public string? Category { get; set; }
        public string? AiCategory { get; set; }
        public string? AiRisk { get; set; }
        public string? Memo { get; set; }
        public string? TaxType { get; set; }
        public string? AccountTitle { get; set; }
    }

    public class UpdateReceiptRequest
    {
        public string? Store { get; set; }
        public string? Date { get; set; }
        public string? Address { get; set; }
        public string? Tel { get; set; }
        public string? PaymentMethod { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? CreditAccount { get; set; }
    }

    public class CreateReceiptRequest
    {
        public required string Store { get; set; }
        public required string Date { get; set; }
        public string? Address { get; set; }
        public string? Tel { get; set; }
        public string? PaymentMethod { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? CreditAccount { get; set; }
        public required List<CreateItemRequest> Items { get; set; }
    }

    public class CreateItemRequest
    {
        public required string Name { get; set; }
        public required int Amount { get; set; }
        public bool? IsTaxReturn { get; set; }
        public string? Category { get; set; }
        public string? AiCategory { get; set; }
        public string? AiRisk { get; set; }
        public string? Memo { get; set; }
        public string? TaxType { get; set; }
        public string? AccountTitle { get; set; }
    }
}

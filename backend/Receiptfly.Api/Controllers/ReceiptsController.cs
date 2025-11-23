using MediatR;
using Microsoft.AspNetCore.Mvc;
using Receiptfly.Application.Commands.CreateReceipt;
using Receiptfly.Application.Commands.UpdateReceipt;
using Receiptfly.Application.Commands.UpdateTransactionItem;
using Receiptfly.Application.Queries.GetReceiptById;
using Receiptfly.Application.Queries.GetReceipts;
using Receiptfly.Application.Services;

namespace Receiptfly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReceiptsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IReceiptGenerationService _receiptGenerationService;

    public ReceiptsController(IMediator mediator, IReceiptGenerationService receiptGenerationService)
    {
        _mediator = mediator;
        _receiptGenerationService = receiptGenerationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetReceipts()
    {
        var receipts = await _mediator.Send(new GetReceiptsQuery());
        return Ok(receipts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetReceipt(string id)
    {
        // バリデーション: receipt-で始まる必要がある
        if (string.IsNullOrWhiteSpace(id) || !id.StartsWith("receipt-"))
        {
            return BadRequest(new { error = "Invalid receipt ID format. Expected format: receipt-{uuid}" });
        }

        var receipt = await _mediator.Send(new GetReceiptByIdQuery(id));

        if (receipt == null)
        {
            return NotFound();
        }

        return Ok(receipt);
    }

    [HttpPut("{id}/items/{itemId}")]
    public async Task<IActionResult> UpdateItem(string id, string itemId, [FromBody] UpdateItemRequest request)
    {
        // バリデーション
        if (string.IsNullOrWhiteSpace(id) || !id.StartsWith("receipt-"))
        {
            return BadRequest(new { error = "Invalid receipt ID format. Expected format: receipt-{uuid}" });
        }
        if (string.IsNullOrWhiteSpace(itemId) || !itemId.StartsWith("transaction-"))
        {
            return BadRequest(new { error = "Invalid transaction item ID format. Expected format: transaction-{uuid}" });
        }

        var command = new UpdateTransactionItemCommand(
            id,
            itemId,
            request.Name,
            request.Amount,
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
    public async Task<IActionResult> UpdateReceipt(string id, [FromBody] UpdateReceiptRequest request)
    {
        // バリデーション
        if (string.IsNullOrWhiteSpace(id) || !id.StartsWith("receipt-"))
        {
            return BadRequest(new { error = "Invalid receipt ID format. Expected format: receipt-{uuid}" });
        }

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

    [HttpPost("from-ocr")]
    public async Task<IActionResult> GenerateReceiptFromOcr([FromBody] GenerateReceiptFromOcrRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OcrText))
        {
            return BadRequest(new { error = "OCR text is required" });
        }

        if (request.AccountTitles == null || request.AccountTitles.Count == 0)
        {
            return BadRequest(new { error = "Account titles are required" });
        }

        if (request.Categories == null || request.Categories.Count == 0)
        {
            return BadRequest(new { error = "Categories are required" });
        }

        try
        {
            var receiptData = await _receiptGenerationService.GenerateReceiptFromOcrAsync(
                request.OcrText,
                request.AccountTitles,
                request.Categories
            );

            var createCommand = new CreateReceiptCommand(
                receiptData.Store,
                receiptData.Date,
                receiptData.Tel,
                receiptData.PaymentMethod,
                receiptData.Address,
                receiptData.RegistrationNumber,
                receiptData.CreditAccount,
                receiptData.Items.Select(item => new CreateReceiptItemDto(
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

            var receipt = await _mediator.Send(createCommand);

            return CreatedAtAction(nameof(GetReceipt), new { id = receipt.Id }, receipt);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to generate receipt from OCR", message = ex.Message });
        }
    }

    [HttpPost("batch-from-ocr")]
    public async Task<IActionResult> GenerateReceiptsFromOcrBatch([FromBody] BatchGenerateReceiptFromOcrRequest request)
    {
        if (request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(new { error = "At least one OCR item is required" });
        }

        if (request.AccountTitles == null || request.AccountTitles.Count == 0)
        {
            return BadRequest(new { error = "Account titles are required" });
        }

        if (request.Categories == null || request.Categories.Count == 0)
        {
            return BadRequest(new { error = "Categories are required" });
        }

        var results = new List<BatchReceiptResult>();

        foreach (var item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.OcrText))
            {
                results.Add(new BatchReceiptResult
                {
                    FileName = item.FileName ?? "unknown",
                    Success = false,
                    Error = "OCR text is required"
                });
                continue;
            }

            try
            {
                var receiptData = await _receiptGenerationService.GenerateReceiptFromOcrAsync(
                    item.OcrText,
                    request.AccountTitles,
                    request.Categories
                );

                var createCommand = new CreateReceiptCommand(
                    receiptData.Store,
                    receiptData.Date,
                    receiptData.Tel,
                    receiptData.PaymentMethod,
                    receiptData.Address,
                    receiptData.RegistrationNumber,
                    receiptData.CreditAccount,
                    receiptData.Items.Select(item => new CreateReceiptItemDto(
                        item.Name,
                        item.Amount,
                        item.IsTaxReturn,
                        item.Category,
                        item.AiCategory,
                        item.AiRisk,
                        item.Memo,
                        item.TaxType,
                        item.AccountTitle
                    )).ToList(),
                    item.FileName
                );

                var receipt = await _mediator.Send(createCommand);

                results.Add(new BatchReceiptResult
                {
                    FileName = item.FileName ?? "unknown",
                    Success = true,
                    ReceiptId = receipt.Id,
                    Receipt = receipt
                });
            }
            catch (Exception ex)
            {
                results.Add(new BatchReceiptResult
                {
                    FileName = item.FileName ?? "unknown",
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        return Ok(new
        {
            total = request.Items.Count,
            succeeded = results.Count(r => r.Success),
            failed = results.Count(r => !r.Success),
            results = results
        });
    }

    public class UpdateItemRequest
    {
        public string? Name { get; set; }
        public int? Amount { get; set; }
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

    public class GenerateReceiptFromOcrRequest
    {
        public required string OcrText { get; set; }
        public required List<string> AccountTitles { get; set; }
        public required List<string> Categories { get; set; }
    }

    public class BatchGenerateReceiptFromOcrRequest
    {
        public required List<OcrItem> Items { get; set; }
        public required List<string> AccountTitles { get; set; }
        public required List<string> Categories { get; set; }
    }

    public class OcrItem
    {
        public required string OcrText { get; set; }
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
    }

    public class BatchReceiptResult
    {
        public required string FileName { get; set; }
        public required bool Success { get; set; }
        public string? ReceiptId { get; set; }
        public object? Receipt { get; set; }
        public string? Error { get; set; }
    }
}

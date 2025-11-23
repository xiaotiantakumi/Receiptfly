using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MediatR;
using Receiptfly.Application.Commands.CreateReceipt;
using Receiptfly.Application.Commands.UpdateReceipt;
using Receiptfly.Application.Commands.UpdateTransactionItem;
using Receiptfly.Application.Queries.GetReceiptById;
using Receiptfly.Application.Queries.GetReceipts;
using Receiptfly.Application.Services;
using System.Text.Json;

namespace Receiptfly.Functions
{
    public class ReceiptFunctions
    {
        private readonly ILogger<ReceiptFunctions> _logger;
        private readonly IMediator _mediator;
        private readonly IReceiptGenerationService _receiptGenerationService;

        public ReceiptFunctions(ILogger<ReceiptFunctions> logger, IMediator mediator, IReceiptGenerationService receiptGenerationService)
        {
            _logger = logger;
            _mediator = mediator;
            _receiptGenerationService = receiptGenerationService;
        }

        [Function("GetReceipts")]
        public async Task<IActionResult> GetReceipts([HttpTrigger(AuthorizationLevel.Function, "get", Route = "receipts")] HttpRequest req)
        {
            _logger.LogInformation("Getting all receipts.");
            var receipts = await _mediator.Send(new GetReceiptsQuery());
            return new OkObjectResult(receipts);
        }

        [Function("GetReceipt")]
        public async Task<IActionResult> GetReceipt([HttpTrigger(AuthorizationLevel.Function, "get", Route = "receipts/{id}")] HttpRequest req, string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !id.StartsWith("receipt-"))
            {
                return new BadRequestObjectResult("Invalid receipt ID format. Expected format: receipt-{uuid}");
            }

            _logger.LogInformation($"Getting receipt {id}.");
            var receipt = await _mediator.Send(new GetReceiptByIdQuery(id));

            if (receipt == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(receipt);
        }

        [Function("UpdateItem")]
        public async Task<IActionResult> UpdateItem(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "receipts/{id}/items/{itemId}")] HttpRequest req,
            string id, string itemId)
        {
            if (string.IsNullOrWhiteSpace(id) || !id.StartsWith("receipt-"))
            {
                return new BadRequestObjectResult("Invalid receipt ID format. Expected format: receipt-{uuid}");
            }
            if (string.IsNullOrWhiteSpace(itemId) || !itemId.StartsWith("transaction-"))
            {
                return new BadRequestObjectResult("Invalid transaction item ID format. Expected format: transaction-{uuid}");
            }

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<UpdateItemRequest>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (request == null)
            {
                return new BadRequestObjectResult("Invalid request body.");
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
                return new NotFoundResult();
            }

            return new NoContentResult();
        }

        [Function("UpdateReceipt")]
        public async Task<IActionResult> UpdateReceipt(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "receipts/{id}")] HttpRequest req,
            string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !id.StartsWith("receipt-"))
            {
                return new BadRequestObjectResult("Invalid receipt ID format. Expected format: receipt-{uuid}");
            }

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<UpdateReceiptRequest>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (request == null)
            {
                return new BadRequestObjectResult("Invalid request body.");
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
                return new NotFoundResult();
            }

            return new NoContentResult();
        }

        [Function("CreateReceipt")]
        public async Task<IActionResult> CreateReceipt([HttpTrigger(AuthorizationLevel.Function, "post", Route = "receipts")] HttpRequest req)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<CreateReceiptRequest>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (request == null)
            {
                return new BadRequestObjectResult("Invalid request body.");
            }

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

            // CreatedAtAction is harder in Functions because we don't have easy route generation, 
            // but we can return CreatedResult with a location header if we want, or just the object.
            // For simplicity, returning the object with 201 status.
            return new CreatedResult($"/api/receipts/{receipt.Id}", receipt);
        }

        [Function("GenerateReceiptFromOcr")]
        public async Task<IActionResult> GenerateReceiptFromOcr([HttpTrigger(AuthorizationLevel.Function, "post", Route = "receipts/from-ocr")] HttpRequest req)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<GenerateReceiptFromOcrRequest>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (request == null)
            {
                 return new BadRequestObjectResult(new { error = "Invalid request body" });
            }

            if (string.IsNullOrWhiteSpace(request.OcrText))
            {
                return new BadRequestObjectResult(new { error = "OCR text is required" });
            }

            if (request.AccountTitles == null || request.AccountTitles.Count == 0)
            {
                return new BadRequestObjectResult(new { error = "Account titles are required" });
            }

            if (request.Categories == null || request.Categories.Count == 0)
            {
                return new BadRequestObjectResult(new { error = "Categories are required" });
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

                return new CreatedResult($"/api/receipts/{receipt.Id}", receipt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate receipt from OCR");
                return new ObjectResult(new { error = "Failed to generate receipt from OCR", message = ex.Message }) { StatusCode = 500 };
            }
        }

        [Function("GenerateReceiptsFromOcrBatch")]
        public async Task<IActionResult> GenerateReceiptsFromOcrBatch([HttpTrigger(AuthorizationLevel.Function, "post", Route = "receipts/batch-from-ocr")] HttpRequest req)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<BatchGenerateReceiptFromOcrRequest>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (request == null)
            {
                 return new BadRequestObjectResult(new { error = "Invalid request body" });
            }

            if (request.Items == null || request.Items.Count == 0)
            {
                return new BadRequestObjectResult(new { error = "At least one OCR item is required" });
            }

            if (request.AccountTitles == null || request.AccountTitles.Count == 0)
            {
                return new BadRequestObjectResult(new { error = "Account titles are required" });
            }

            if (request.Categories == null || request.Categories.Count == 0)
            {
                return new BadRequestObjectResult(new { error = "Categories are required" });
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
                        )).ToList()
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

            return new OkObjectResult(new
            {
                total = request.Items.Count,
                succeeded = results.Count(r => r.Success),
                failed = results.Count(r => !r.Success),
                results = results
            });
        }

        // DTOs
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
}

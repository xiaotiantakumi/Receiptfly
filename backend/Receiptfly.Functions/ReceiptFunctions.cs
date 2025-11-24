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
using System.Text.Json;

namespace Receiptfly.Functions
{
    public class ReceiptFunctions
    {
        private readonly ILogger<ReceiptFunctions> _logger;
        private readonly IMediator _mediator;

        public ReceiptFunctions(ILogger<ReceiptFunctions> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        [Function("GetReceipts")]
        public async Task<IActionResult> GetReceipts([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "receipts")] HttpRequest req)
        {
            _logger.LogInformation("Getting all receipts.");
            var receipts = await _mediator.Send(new GetReceiptsQuery());
            return new OkObjectResult(receipts);
        }

        [Function("GetReceipt")]
        public async Task<IActionResult> GetReceipt([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "receipts/{id}")] HttpRequest req, string id)
        {
            if (!Guid.TryParse(id, out var receiptId))
            {
                return new BadRequestObjectResult("Invalid receipt ID.");
            }

            _logger.LogInformation($"Getting receipt {id}.");
            var receipt = await _mediator.Send(new GetReceiptByIdQuery(receiptId));

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
            if (!Guid.TryParse(id, out var receiptId) || !Guid.TryParse(itemId, out var transactionItemId))
            {
                return new BadRequestObjectResult("Invalid ID format.");
            }

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<UpdateItemRequest>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (request == null)
            {
                return new BadRequestObjectResult("Invalid request body.");
            }

            var command = new UpdateTransactionItemCommand(
                receiptId,
                transactionItemId,
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
            if (!Guid.TryParse(id, out var receiptId))
            {
                return new BadRequestObjectResult("Invalid receipt ID.");
            }

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<UpdateReceiptRequest>(requestBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (request == null)
            {
                return new BadRequestObjectResult("Invalid request body.");
            }

            var command = new UpdateReceiptCommand(
                receiptId,
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

    }
}

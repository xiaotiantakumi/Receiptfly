using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Receiptfly.Api.Data;
using Receiptfly.Api.Models;

namespace Receiptfly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReceiptsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ReceiptsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Receipt>>> GetReceipts()
    {
        return await _context.Receipts.Include(r => r.Items).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Receipt>> GetReceipt(int id)
    {
        var receipt = await _context.Receipts.Include(r => r.Items).FirstOrDefaultAsync(r => r.Id == id);

        if (receipt == null)
        {
            return NotFound();
        }

        return receipt;
    }

    [HttpPut("{id}/items/{itemId}")]
    public async Task<IActionResult> UpdateItem(int id, int itemId, [FromBody] UpdateItemRequest request)
    {
        var item = await _context.TransactionItems.FindAsync(itemId);

        if (item == null)
        {
            return NotFound();
        }

        if (item.ReceiptId != id)
        {
            return BadRequest("Item does not belong to the specified receipt.");
        }

        // Update fields if provided
        if (request.IsTaxReturn.HasValue) item.IsTaxReturn = request.IsTaxReturn.Value;
        if (request.Category != null) item.Category = request.Category;
        if (request.AiCategory != null) item.AiCategory = request.AiCategory;
        if (request.AiRisk != null) item.AiRisk = request.AiRisk;
        if (request.Memo != null) item.Memo = request.Memo;
        if (request.TaxType != null) item.TaxType = request.TaxType;
        if (request.AccountTitle != null) item.AccountTitle = request.AccountTitle;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateReceipt(int id, [FromBody] UpdateReceiptRequest request)
    {
        var receipt = await _context.Receipts.FindAsync(id);

        if (receipt == null)
        {
            return NotFound();
        }

        if (request.Store != null) receipt.Store = request.Store;
        if (request.Date != null) receipt.Date = request.Date;
        if (request.Address != null) receipt.Address = request.Address;
        if (request.Tel != null) receipt.Tel = request.Tel;
        if (request.PaymentMethod != null) receipt.PaymentMethod = request.PaymentMethod;
        if (request.RegistrationNumber != null) receipt.RegistrationNumber = request.RegistrationNumber;
        if (request.CreditAccount != null) receipt.CreditAccount = request.CreditAccount;

        await _context.SaveChangesAsync();

        return NoContent();
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
}

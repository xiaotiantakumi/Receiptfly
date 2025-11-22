using Microsoft.EntityFrameworkCore;
using Receiptfly.Api.Models;

namespace Receiptfly.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Receipt> Receipts { get; set; }
    public DbSet<TransactionItem> TransactionItems { get; set; }
}

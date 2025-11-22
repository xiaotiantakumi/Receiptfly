using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Receiptfly.Infrastructure.Data;

#nullable disable

namespace Receiptfly.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        /// <inheritdoc />
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.0");

            modelBuilder.Entity("Receiptfly.Domain.Entities.Receipt", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Address")
                        .HasColumnType("TEXT");

                    b.Property<string>("CreditAccount")
                        .HasColumnType("TEXT");

                    b.Property<string>("Date")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("PaymentMethod")
                        .HasColumnType("TEXT");

                    b.Property<string>("RegistrationNumber")
                        .HasColumnType("TEXT");

                    b.Property<string>("Store")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Tel")
                        .HasColumnType("TEXT");

                    b.Property<int>("Total")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("Receipts");
                });

            modelBuilder.Entity("Receiptfly.Domain.Entities.TransactionItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("AccountTitle")
                        .HasColumnType("TEXT");

                    b.Property<string>("AiCategory")
                        .HasColumnType("TEXT");

                    b.Property<string>("AiRisk")
                        .HasColumnType("TEXT");

                    b.Property<int>("Amount")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Category")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsTaxReturn")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Memo")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ReceiptId")
                        .HasColumnType("TEXT");

                    b.Property<string>("TaxType")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ReceiptId");

                    b.ToTable("TransactionItems");
                });

            modelBuilder.Entity("Receiptfly.Domain.Entities.TransactionItem", b =>
                {
                    b.HasOne("Receiptfly.Domain.Entities.Receipt", "Receipt")
                        .WithMany("Items")
                        .HasForeignKey("ReceiptId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Receipt");
                });

            modelBuilder.Entity("Receiptfly.Domain.Entities.Receipt", b =>
                {
                    b.Navigation("Items");
                });
#pragma warning restore 612, 618
        }
    }
}


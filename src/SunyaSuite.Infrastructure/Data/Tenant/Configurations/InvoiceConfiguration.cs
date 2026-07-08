using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunyaSuite.Domain.Entities.Tenant;

namespace SunyaSuite.Infrastructure.Data.Tenant.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.BranchId);

        builder.Property(x => x.InvoiceNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.DateBS).HasMaxLength(15);
        builder.Property(x => x.Subtotal).HasPrecision(18, 2);
        builder.Property(x => x.TaxRate).HasPrecision(5, 2);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.Property(x => x.VatAmount).HasPrecision(18, 2);
        builder.Property(x => x.Total).HasPrecision(18, 2);
        builder.Property(x => x.GrandTotalInWords).HasMaxLength(300);
        builder.Property(x => x.BuyerPan).HasMaxLength(15);
        builder.Property(x => x.BuyerAddress).HasMaxLength(500);
        builder.Property(x => x.SellerName).HasMaxLength(200);
        builder.Property(x => x.SellerPan).HasMaxLength(15);
        builder.Property(x => x.SellerAddress).HasMaxLength(500);
        builder.Property(x => x.SellerPhone).HasMaxLength(50);
        builder.Property(x => x.SellerLogoBase64).HasColumnType("text");
        builder.Property(x => x.AmountPaid).HasPrecision(18, 2);
        builder.Property(x => x.RowVersion).IsRowVersion().IsRequired();

        builder.Property(x => x.BillType)
            .HasConversion<int>();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeletedAt);

        builder.HasIndex(x => x.InvoiceNumber).IsUnique();
        builder.HasIndex(x => x.ClientId);
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.FiscalYearId);
        builder.HasIndex(x => x.DueDate);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => new { x.CompanyId, x.IsDeleted });
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.CompanyInfo)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BranchInfo)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Client)
            .WithMany(x => x.Invoices)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.Invoice)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ReceiptAllocations)
            .WithOne(x => x.Invoice)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunyaSuite.Domain.Entities.Tenant;

namespace SunyaSuite.Infrastructure.Data.Tenant.Configurations;

public class MoneyReceiptConfiguration : IEntityTypeConfiguration<MoneyReceipt>
{
    public void Configure(EntityTypeBuilder<MoneyReceipt> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.BranchId);

        builder.Property(x => x.ReceiptNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.DateBS).HasMaxLength(15);
        builder.Property(x => x.ReceivedFromName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ReceivedFromPan).HasMaxLength(15);
        builder.Property(x => x.ReceivedFromAddress).HasMaxLength(500);
        builder.Property(x => x.AmountReceived).HasPrecision(18, 2).IsRequired();
        builder.Property(x => x.AmountInWords).HasMaxLength(300);
        builder.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ReferenceNo).HasMaxLength(100);
        builder.Property(x => x.ReceivedBy).HasMaxLength(200);
        builder.Property(x => x.RowVersion).IsRowVersion().IsRequired();
        builder.Property(x => x.SellerLogoBase64).HasColumnType("text");
        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeletedAt);

        builder.HasIndex(x => x.ReceiptNumber).IsUnique();
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.FiscalYearId);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.CompanyInfo)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BranchInfo)
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Allocations)
            .WithOne(x => x.MoneyReceipt)
            .HasForeignKey(x => x.MoneyReceiptId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

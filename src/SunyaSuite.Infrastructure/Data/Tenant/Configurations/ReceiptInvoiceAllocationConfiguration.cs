using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunyaSuite.Domain.Entities.Tenant;

namespace SunyaSuite.Infrastructure.Data.Tenant.Configurations;

public class ReceiptInvoiceAllocationConfiguration : IEntityTypeConfiguration<ReceiptInvoiceAllocation>
{
    public void Configure(EntityTypeBuilder<ReceiptInvoiceAllocation> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AllocatedAmount).HasPrecision(18, 2).IsRequired();

        builder.HasOne(x => x.MoneyReceipt)
            .WithMany(x => x.Allocations)
            .HasForeignKey(x => x.MoneyReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Invoice)
            .WithMany(x => x.ReceiptAllocations)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.MoneyReceiptId);
        builder.HasIndex(x => x.InvoiceId);
    }
}

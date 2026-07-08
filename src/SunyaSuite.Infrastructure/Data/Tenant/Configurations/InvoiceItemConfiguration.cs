using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunyaSuite.Domain.Entities.Tenant;

namespace SunyaSuite.Infrastructure.Data.Tenant.Configurations;

public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description).HasMaxLength(500).IsRequired();
        builder.Property(x => x.HsCode).HasMaxLength(10);
        builder.Property(x => x.Unit).HasMaxLength(20);
        builder.Property(x => x.Quantity).HasPrecision(18, 2);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 2);

        builder.HasOne(x => x.Invoice)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.InvoiceId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

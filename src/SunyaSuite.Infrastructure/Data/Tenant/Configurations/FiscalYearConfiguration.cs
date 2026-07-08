using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunyaSuite.Domain.Entities.Tenant;

namespace SunyaSuite.Infrastructure.Data.Tenant.Configurations;

public class FiscalYearConfiguration : IEntityTypeConfiguration<FiscalYear>
{
    public void Configure(EntityTypeBuilder<FiscalYear> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CompanyId).IsRequired();

        builder.Property(x => x.YearName).HasMaxLength(10).IsRequired();
        builder.Property(x => x.StartDateBS).HasMaxLength(15).IsRequired();
        builder.Property(x => x.EndDateBS).HasMaxLength(15).IsRequired();
        builder.Property(x => x.IsOpen).IsRequired();
        builder.Property(x => x.IsCurrent).IsRequired();
        builder.Property(x => x.CreatedAt);

        builder.HasIndex(x => x.YearName).IsUnique();
        builder.HasIndex(x => x.CompanyId);

        builder.HasOne(x => x.CompanyInfo)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

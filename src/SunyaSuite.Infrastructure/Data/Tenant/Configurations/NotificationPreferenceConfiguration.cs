using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunyaSuite.Domain.Entities.Tenant;

namespace SunyaSuite.Infrastructure.Data.Tenant.Configurations;

public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CompanyId).IsRequired();

        builder.Property(x => x.UserId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(50).IsRequired();
        builder.Property(x => x.EmailEnabled).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.Type }).IsUnique();

        builder.HasOne(x => x.CompanyInfo)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

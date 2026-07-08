using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunyaSuite.Domain.Entities.Tenant;

namespace SunyaSuite.Infrastructure.Data.Tenant.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.BranchId);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.IsDeleted).IsRequired();
        builder.Property(x => x.DeletedAt);
        builder.HasIndex(x => x.IsDeleted);
        builder.HasIndex(x => x.CompanyId);
        builder.HasIndex(x => x.ClientId);
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
            .WithMany(x => x.Projects)
            .HasForeignKey(x => x.ClientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

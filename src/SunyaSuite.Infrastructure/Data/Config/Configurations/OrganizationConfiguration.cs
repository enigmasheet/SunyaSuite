using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SunyaSuite.Domain.Entities.Config;

namespace SunyaSuite.Infrastructure.Data.Config.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ConnectionString).HasMaxLength(500).IsRequired(false);
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}

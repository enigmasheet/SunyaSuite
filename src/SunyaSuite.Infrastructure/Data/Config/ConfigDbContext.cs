using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SunyaSuite.Domain.Entities.Config;

namespace SunyaSuite.Infrastructure.Data.Config;

public class ConfigDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly TimeProvider _timeProvider;

    public ConfigDbContext(DbContextOptions<ConfigDbContext> options, TimeProvider timeProvider) : base(options)
    {
        _timeProvider = timeProvider;
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationUser> OrganizationUsers => Set<OrganizationUser>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ConfigDbContext).Assembly,
            t => t.Namespace == typeof(Configurations.OrganizationConfiguration).Namespace);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is EntityState.Added)
            {
                if (entry.Entity is Organization org && org.CreatedAt == default)
                    org.CreatedAt = utcNow;
                if (entry.Entity is ApplicationUser user && user.CreatedAt == default)
                    user.CreatedAt = utcNow;
                if (entry.Entity is OrganizationUser ou && ou.JoinedAt == default)
                    ou.JoinedAt = utcNow;
            }

            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is DateTime dt && dt.Kind != DateTimeKind.Utc)
                    property.CurrentValue = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
            }
        }
        return await base.SaveChangesAsync(ct);
    }
}

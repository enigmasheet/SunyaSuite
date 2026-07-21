using Microsoft.EntityFrameworkCore;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Infrastructure.Data.Tenant;
using System.Collections.Concurrent;

namespace SunyaSuite.Infrastructure.Services.Config;

public class TenantDbContextFactory : IDbContextFactory<ApplicationDbContext>
{
    private static readonly ConcurrentDictionary<string, byte> _migrated = new();

    private readonly ITenantContext _tenantContext;
    private readonly DbContextOptions<ApplicationDbContext> _baseOptions;
    private readonly TimeProvider _timeProvider;

    public TenantDbContextFactory(
        ITenantContext tenantContext,
        DbContextOptions<ApplicationDbContext> baseOptions,
        TimeProvider timeProvider)
    {
        _tenantContext = tenantContext;
        _baseOptions = baseOptions;
        _timeProvider = timeProvider;
    }

    public ApplicationDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>(_baseOptions);

        if (_tenantContext.HasTenant && !string.IsNullOrEmpty(_tenantContext.ConnectionString))
        {
            optionsBuilder.UseNpgsql(_tenantContext.ConnectionString,
                npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null));
        }

        var ctx = new ApplicationDbContext(optionsBuilder.Options, _timeProvider);
        if (_tenantContext.CompanyId.HasValue)
            ctx.SetCompanyId(_tenantContext.CompanyId.Value);
        return ctx;
    }

    public async ValueTask<ApplicationDbContext> CreateDbContextAsync(CancellationToken ct = default)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>(_baseOptions);

        if (_tenantContext.HasTenant && !string.IsNullOrEmpty(_tenantContext.ConnectionString))
        {
            optionsBuilder.UseNpgsql(_tenantContext.ConnectionString,
                npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null));

            if (_migrated.TryAdd(_tenantContext.ConnectionString, 0))
            {
                await using var migrator = new ApplicationDbContext(optionsBuilder.Options, _timeProvider);
                await migrator.Database.MigrateAsync(ct);
            }
        }

        var ctx = new ApplicationDbContext(optionsBuilder.Options, _timeProvider);
        if (_tenantContext.CompanyId.HasValue)
            ctx.SetCompanyId(_tenantContext.CompanyId.Value);
        return ctx;
    }
}

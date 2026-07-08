using Microsoft.EntityFrameworkCore;
using SunyaSuite.Infrastructure.Data.Config;
using SunyaSuite.Infrastructure.Data.Tenant;
using SunyaSuite.Infrastructure.DataSeeding;

namespace SunyaSuite.Infrastructure.Services.Admin;

public class DatabaseResetService
{
    private readonly IDbContextFactory<ConfigDbContext> _configFactory;
    private readonly IDbContextFactory<ApplicationDbContext> _tenantFactory;
    private readonly IServiceProvider _serviceProvider;

    public DatabaseResetService(
        IDbContextFactory<ConfigDbContext> configFactory,
        IDbContextFactory<ApplicationDbContext> tenantFactory,
        IServiceProvider serviceProvider)
    {
        _configFactory = configFactory;
        _tenantFactory = tenantFactory;
        _serviceProvider = serviceProvider;
    }

    public async Task ResetAsync(CancellationToken ct = default)
    {
        // Drop and recreate schemas in both databases
        await DropSchemaAsync(_configFactory, ct);
        await DropSchemaAsync(_tenantFactory, ct);

        // Re-run migrations
        await using (var ctx = await _configFactory.CreateDbContextAsync(ct))
            await ctx.Database.MigrateAsync(ct);

        await using (var ctx = await _tenantFactory.CreateDbContextAsync(ct))
            await ctx.Database.MigrateAsync(ct);

        // Re-seed
        await SeedData.InitializeAsync(_serviceProvider);
    }

    private static async Task DropSchemaAsync<T>(IDbContextFactory<T> factory, CancellationToken ct) where T : DbContext
    {
        await using var ctx = await factory.CreateDbContextAsync(ct);
        await ctx.Database.ExecuteSqlRawAsync("DROP SCHEMA IF EXISTS public CASCADE;", ct);
        await ctx.Database.ExecuteSqlRawAsync("CREATE SCHEMA public;", ct);
    }
}

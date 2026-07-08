using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SunyaSuite.Application.Settings;
using SunyaSuite.Infrastructure.Data.Config;
using SunyaSuite.Infrastructure.Data.Tenant;

namespace SunyaSuite.Infrastructure.Services.Admin;

public class ApplyTenantMigrationsService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ApplyTenantMigrationsService> _logger;

    public ApplyTenantMigrationsService(
        IServiceScopeFactory scopeFactory,
        ILogger<ApplyTenantMigrationsService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;
        var configFactory = sp.GetRequiredService<IDbContextFactory<ConfigDbContext>>();
        var dbSettings = sp.GetRequiredService<IOptions<DatabaseSettings>>();
        var timeProvider = sp.GetRequiredService<TimeProvider>();

        await using var configDb = await configFactory.CreateDbContextAsync(cancellationToken);
        var orgs = await configDb.Organizations
            .Where(o => o.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var baseConnection = dbSettings.Value.TemplateConnection;
        var connectionStrings = orgs
            .Select(o => o.ConnectionString ?? baseConnection)
            .Distinct()
            .ToList();

        _logger.LogInformation("Migrating {Count} tenant database(s)...", connectionStrings.Count);

        foreach (var connStr in connectionStrings)
        {
            if (cancellationToken.IsCancellationRequested) return;

            try
            {
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                optionsBuilder.UseNpgsql(connStr,
                    npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null));

                await using var ctx = new ApplicationDbContext(optionsBuilder.Options, timeProvider);
                await ctx.Database.MigrateAsync(cancellationToken);

                _logger.LogInformation("Migration applied for tenant database");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Migration failed for tenant database: {ConnectionString}", connStr);
            }
        }

        _logger.LogInformation("All tenant migrations completed.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

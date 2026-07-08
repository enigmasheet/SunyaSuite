using Microsoft.EntityFrameworkCore;
using Serilog;
using SunyaSuite.Infrastructure.Data.Config;
using SunyaSuite.Infrastructure.Data.Tenant;
using SunyaSuite.Infrastructure.DataSeeding;
using SunyaSuite.Infrastructure.Services.Admin;
using SunyaSuite.Web.Api.Middleware;

namespace SunyaSuite.Web.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseStatusCodePages();
        app.UseCors("AllowClient");
        app.UseAuthentication();
        app.UseMiddleware<TenantMiddleware>();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

    public static async Task RunDatabaseStartupAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;

        try
        {
            if (app.Configuration.GetValue<bool>("DatabaseReset:ResetOnStartup"))
            {
                var resetService = sp.GetRequiredService<DatabaseResetService>();
                await resetService.ResetAsync();
            }
            else
            {
                var configFactory = sp.GetRequiredService<IDbContextFactory<ConfigDbContext>>();
                await using (var configCtx = await configFactory.CreateDbContextAsync())
                {
                    await configCtx.Database.MigrateAsync();
                }

                var tenantFactory = sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                await using (var tenantCtx = await tenantFactory.CreateDbContextAsync())
                {
                    await tenantCtx.Database.MigrateAsync();
                }

                await SeedData.InitializeAsync(sp);
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Database migration or seeding failed");
        }
    }
}

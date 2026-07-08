using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SunyaSuite.Application.Interfaces;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Application.Services.Tenant;
using SunyaSuite.Application.Settings;
using SunyaSuite.Infrastructure.Data.Config;
using SunyaSuite.Infrastructure.Data.Tenant;
using SunyaSuite.Infrastructure.HealthChecks;
using SunyaSuite.Infrastructure.Services;
using SunyaSuite.Infrastructure.Services.Admin;
using SunyaSuite.Infrastructure.Services.Config;
using SunyaSuite.Infrastructure.Services.Tenant;

namespace SunyaSuite.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.SectionName));

        // Config database (shared â€” Organizations + Identity)
        services.AddDbContextFactory<ConfigDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("ConfigConnection"),
                npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null))
               .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        // Tenant database â€” register base options (used as fallback when no tenant override)
        services.AddDbContextFactory<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("TemplateConnection"),
                npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null))
               .ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)));

        // Scoped factory â€” resolves tenant-specific connection string per request
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IDbContextFactory<ApplicationDbContext>, TenantDbContextFactory>();

        services.AddHealthChecks()
            .AddCheck<DbContextHealthCheck>("database");

        services.AddScoped<IClientStatusCalculator, ClientStatusCalculator>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IInvoicePdfService, InvoicePdfService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<IMoneyReceiptService, MoneyReceiptService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();
        services.AddScoped<INepaliDateService, NepaliDateService>();
        services.AddScoped<IUserPreferenceService, UserPreferenceService>();
        services.AddScoped<IBusinessProfileService, BusinessProfileService>();
        services.AddScoped<INumberToWordsService, NumberToWordsService>();
        services.AddScoped<IFiscalYearService, FiscalYearService>();
        services.AddScoped<ICompanyService, CompanyService>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IReceiptPdfService, ReceiptPdfService>();
        services.AddScoped<IInviteService, InviteService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<DatabaseResetService>();
        services.AddHostedService<ApplyTenantMigrationsService>();

        return services;
    }
}

using Microsoft.AspNetCore.Components.Authorization;
using SunyaSuite.Application.Interfaces;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Constants;
using SunyaSuite.Domain.Enums;
using SunyaSuite.Web.Client.Auth;
using SunyaSuite.Web.Client.Services;

namespace SunyaSuite.Web.Client.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthServices(this IServiceCollection services)
    {
        services.AddSingleton<TokenManager>();
        services.AddSingleton<OrgManager>();
        services.AddSingleton<JwtAuthenticationStateProvider>();
        services.AddSingleton<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());
        services.AddTransient<AuthMessageHandler>();
        services.AddAuthorizationCore(options =>
        {
            options.AddPolicy(PolicyNames.SystemAdminOnly, p => p.RequireRole(RoleNames.SystemAdmin));
            options.AddPolicy(PolicyNames.OrgAdminOrAbove, p => p.RequireRole(OrgRoles.Owner, OrgRoles.OrgAdmin));
            options.AddPolicy(PolicyNames.OrgMemberOrAbove, p => p.RequireRole(OrgRoles.Owner, OrgRoles.OrgAdmin, OrgRoles.Member));
            options.AddPolicy(PolicyNames.OrgViewerOrAbove, p => p.RequireRole(OrgRoles.Owner, OrgRoles.OrgAdmin, OrgRoles.Member, OrgRoles.Viewer));
        });

        return services;
    }

    public static IServiceCollection AddHttpClients(this IServiceCollection services, string apiUrl)
    {
        services.AddHttpClient("Api", client =>
            client.BaseAddress = new Uri(apiUrl))
            .AddHttpMessageHandler<AuthMessageHandler>();

        services.AddHttpClient("Renew", client =>
            client.BaseAddress = new Uri(apiUrl));

        services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiUrl) });

        return services;
    }

    public static IServiceCollection AddMenuService(this IServiceCollection services)
    {
        services.AddScoped<IMenuService>(sp =>
        {
            var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api");
            return new MenuService(http);
        });
        return services;
    }

    public static IServiceCollection AddAppServiceClients(this IServiceCollection services)
    {
        // Config service clients
        RegisterClientService<ISystemDashboardService, SystemDashboardServiceClient>(services);
        RegisterClientService<IUserService, UserServiceClient>(services);
        RegisterClientService<IAuditService, AuditServiceClient>(services);
        RegisterClientService<IUserPreferenceService, UserPreferenceServiceClient>(services);
        RegisterClientService<IOrganizationService, OrganizationServiceClient>(services);
        RegisterClientService<IInviteService, InviteServiceClient>(services);

        // Tenant service clients
        RegisterClientService<IClientService, ClientServiceClient>(services);
        RegisterClientService<IInvoiceService, InvoiceServiceClient>(services);
        RegisterClientService<IProjectService, ProjectServiceClient>(services);
        RegisterClientService<IMoneyReceiptService, MoneyReceiptServiceClient>(services);
        RegisterClientService<IFiscalYearService, FiscalYearServiceClient>(services);
        RegisterClientService<IDashboardService, DashboardServiceClient>(services);
        RegisterClientService<INotificationPreferenceService, NotificationPreferenceServiceClient>(services);

        RegisterClientService<IInvoicePdfService, InvoicePdfServiceClient>(services);
        RegisterClientService<IReceiptPdfService, ReceiptPdfServiceClient>(services);
        RegisterClientService<IExportService, ExportServiceClient>(services);
        RegisterClientService<IEmailService, EmailServiceClient>(services);
        RegisterClientService<ICompanyService, CompanyServiceClient>(services);
        RegisterClientService<IBranchService, BranchServiceClient>(services);

        return services;
    }

    private static void RegisterClientService<TInterface, TImplementation>(IServiceCollection services)
        where TInterface : class
        where TImplementation : class, TInterface
    {
        services.AddScoped<TInterface>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api");
            return ActivatorUtilities.CreateInstance<TImplementation>(sp, httpClient);
        });
    }
}

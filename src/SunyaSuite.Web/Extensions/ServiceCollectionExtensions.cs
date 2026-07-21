using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SunyaSuite.Application.Settings;
using SunyaSuite.Domain.Constants;
using SunyaSuite.Domain.Entities.Config;
using SunyaSuite.Infrastructure.Data.Config;
using SunyaSuite.Infrastructure.Services.Tenant;
using SunyaSuite.Web.Api.Auth;
using SunyaSuite.Web.Api.Services.Config;
using System.Text;

namespace SunyaSuite.Web.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowClient", policy =>
            {
                var origins = configuration.GetSection("ClientUrls").Get<string[]>()
                    ?? [configuration.GetValue<string>("ClientUrl") ?? "http://localhost:5002"];
                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? new JwtSettings();
        var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };
        });

        return services;
    }

    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<AuthenticationStateProvider, ApiAuthStateProvider>();

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.SignIn.RequireConfirmedAccount = true;
            options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
        })
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<ConfigDbContext>()
        .AddSignInManager()
        .AddDefaultTokenProviders();

        return services;
    }

    public static IServiceCollection AddAppAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, OrgRoleAuthorizationHandler>();

        services.AddAuthorizationBuilder()
        .AddPolicy(PolicyNames.SystemAdminOnly, p => p.RequireRole(RoleNames.SystemAdmin))
        .AddPolicy(PolicyNames.OrgAdminOrAbove, p => p.AddRequirements(new OrgAdminRequirement()))
        .AddPolicy(PolicyNames.OrgMemberOrAbove, p => p.AddRequirements(new OrgMemberRequirement()))
        .AddPolicy(PolicyNames.OrgViewerOrAbove, p => p.AddRequirements(new OrgViewerRequirement()));

        return services;
    }

    public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<InviteSettings>(configuration.GetSection(InviteSettings.SectionName));
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.Configure<SeedDataSettings>(configuration.GetSection(SeedDataSettings.SectionName));
        services.Configure<VatSettings>(configuration.GetSection(VatSettings.SectionName));
        services.Configure<OverdueSchedulerSettings>(configuration.GetSection(OverdueSchedulerSettings.SectionName));
        services.AddScoped<JwtTokenService>();
        services.AddScoped<IEmailSender<ApplicationUser>, MailKitEmailSender>();
        services.AddHostedService<OverdueBackgroundService>();

        return services;
    }
}

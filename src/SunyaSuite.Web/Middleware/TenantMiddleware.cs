using Microsoft.EntityFrameworkCore;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Infrastructure.Data.Config;
using System.Security.Claims;

namespace SunyaSuite.Web.Api.Middleware;

public class TenantMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> TenantNotRequiredPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/forgot-password",
        "/api/auth/renew",
        "/api/auth/change-password",
        "/api/organizations/my",
        "/api/organizations/deleted",
        "/api/admin/dashboard"
    };

    private static readonly string[] TenantNotRequiredPrefixes =
    [
        "/api/users"
    ];

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, IDbContextFactory<ConfigDbContext> configFactory)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (TenantNotRequiredPaths.Contains(path)
            || TenantNotRequiredPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        var tenantHeader = context.Request.Headers["X-Tenant-ID"].FirstOrDefault();
        if (string.IsNullOrEmpty(tenantHeader))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { message = "Missing X-Tenant-ID header." });
            return;
        }

        await using var configDb = await configFactory.CreateDbContextAsync();
        var org = await configDb.Organizations
            .FirstOrDefaultAsync(o => o.Slug == tenantHeader && o.IsActive);

        if (org is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { message = $"Organization '{tenantHeader}' not found or is inactive." });
            return;
        }

        tenantContext.SetTenant(org.Id, org.Slug, org.ConnectionString);

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is not null)
        {
            var orgUser = await configDb.OrganizationUsers
                .FirstOrDefaultAsync(ou => ou.OrganizationId == org.Id && ou.UserId == userId);

            if (orgUser is not null)
            {
                tenantContext.SetCompany(orgUser.DefaultCompanyId, orgUser.DefaultBranchId);
            }
        }

        await next(context);
    }
}

using Microsoft.EntityFrameworkCore;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Infrastructure.Data.Config;
using System.Security.Claims;

namespace SunyaSuite.Web.Api.Middleware;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, IDbContextFactory<ConfigDbContext> configFactory)
    {
        var tenantHeader = context.Request.Headers["X-Tenant-ID"].FirstOrDefault();
        if (!string.IsNullOrEmpty(tenantHeader))
        {
            await using var configDb = await configFactory.CreateDbContextAsync();
            var org = await configDb.Organizations
                .FirstOrDefaultAsync(o => o.Slug == tenantHeader && o.IsActive);

            if (org is not null)
            {
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
            }
        }

        await _next(context);
    }
}

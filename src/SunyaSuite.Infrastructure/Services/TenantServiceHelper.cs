using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Infrastructure.Data.Tenant;
using System.Security.Claims;

namespace SunyaSuite.Infrastructure.Services;

public static class TenantServiceHelper
{
    public static async Task<Guid> GetRequiredCompanyIdAsync(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ITenantContext tenantContext,
        CancellationToken ct = default)
    {
        if (tenantContext.CompanyId.HasValue)
            return tenantContext.CompanyId.Value;

        await using var context = await contextFactory.CreateDbContextAsync(ct);
        var company = await context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.IsActive, ct);

        return company?.Id ?? throw new InvalidOperationException("No active company found.");
    }

    public static async Task<string> GetCurrentUserIdAsync(AuthenticationStateProvider authStateProvider)
    {
        var state = await authStateProvider.GetAuthenticationStateAsync();
        return state.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
    }

    public static async Task<string> GetCurrentUserNameAsync(AuthenticationStateProvider authStateProvider)
    {
        var state = await authStateProvider.GetAuthenticationStateAsync();
        return state.User.FindFirst(ClaimTypes.Name)?.Value
               ?? state.User.FindFirst("name")?.Value
               ?? "Unknown";
    }
}

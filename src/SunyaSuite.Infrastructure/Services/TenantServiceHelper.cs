using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Infrastructure.Data.Tenant;
using System.Security.Claims;

namespace SunyaSuite.Infrastructure.Services;

public static class TenantServiceHelper
{
    public static Task<Guid> GetRequiredCompanyIdAsync(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ITenantContext tenantContext,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        ArgumentNullException.ThrowIfNull(tenantContext);
        if (tenantContext.CompanyId.HasValue)
            return Task.FromResult(tenantContext.CompanyId.Value);

        throw new InvalidOperationException("No company assigned. Please contact your administrator to set a default company.");
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

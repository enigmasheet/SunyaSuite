using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace SunyaSuite.Web.Api.Auth;

/// <summary>
/// Bridges IHttpContextAccessor (API) to AuthenticationStateProvider (used by infrastructure services).
/// Allows services like ClientService, InvoiceService, etc. to get the current user without Blazor.
/// </summary>
public class ApiAuthStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiAuthStateProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user is null || !user.Identity?.IsAuthenticated == true)
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

        return Task.FromResult(new AuthenticationState(user));
    }
}

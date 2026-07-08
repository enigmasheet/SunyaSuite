using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SunyaSuite.Web.Client.Auth;

public class AuthMessageHandler : DelegatingHandler
{
    private readonly TokenManager _tokenManager;
    private readonly OrgManager _orgManager;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly NavigationManager _navigation;
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly SemaphoreSlim RenewLock = new(1, 1);

    public AuthMessageHandler(
        TokenManager tokenManager,
        OrgManager orgManager,
        AuthenticationStateProvider authStateProvider,
        NavigationManager navigation,
        IHttpClientFactory httpClientFactory)
    {
        _tokenManager = tokenManager;
        _orgManager = orgManager;
        _authStateProvider = authStateProvider;
        _navigation = navigation;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var token = await _tokenManager.GetTokenAsync();
        var hadToken = !string.IsNullOrEmpty(token);
        if (hadToken)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var orgSlug = await _orgManager.GetActiveSlugAsync();
        if (!string.IsNullOrEmpty(orgSlug))
            request.Headers.Add("X-Tenant-ID", orgSlug);

        var response = await base.SendAsync(request, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && hadToken)
        {
            var renewed = await TryRenewTokenAsync(ct);
            if (renewed)
            {
                var newToken = await _tokenManager.GetTokenAsync();
                if (!string.IsNullOrEmpty(newToken))
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

                response = await base.SendAsync(request, ct);
            }
            else
            {
                await _tokenManager.ClearTokenAsync();
                if (_authStateProvider is JwtAuthenticationStateProvider jwtProvider)
                    await jwtProvider.NotifyAuthenticationStateChanged();

                var returnUrl = Uri.EscapeDataString(_navigation.Uri);
                _navigation.NavigateTo($"/login?returnUrl={returnUrl}", forceLoad: true);
            }
        }

        return response;
    }

    private async Task<bool> TryRenewTokenAsync(CancellationToken ct)
    {
        await RenewLock.WaitAsync(ct);
        try
        {
            var token = await _tokenManager.GetTokenAsync();
            if (string.IsNullOrEmpty(token) || !await _tokenManager.IsTokenExpiredAsync())
                return false;

            var renewClient = _httpClientFactory.CreateClient("Renew");
            var renewResponse = await renewClient.PostAsJsonAsync("api/auth/renew", new { token }, ct);

            if (!renewResponse.IsSuccessStatusCode)
                return false;

            var result = await renewResponse.Content.ReadFromJsonAsync<RenewResponse>(cancellationToken: ct);
            if (result is null)
                return false;

            await _tokenManager.RenewTokenAsync(result.Token, result.ExpiresAt);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            RenewLock.Release();
        }
    }

    private record RenewResponse(string Token, DateTime ExpiresAt);
}

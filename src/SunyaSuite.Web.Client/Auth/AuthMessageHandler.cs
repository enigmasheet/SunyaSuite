using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SunyaSuite.Web.Client.Services;

namespace SunyaSuite.Web.Client.Auth;

public class AuthMessageHandler : DelegatingHandler
{
    private readonly TokenManager _tokenManager;
    private readonly OrgManager _orgManager;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly NavigationManager _navigation;
    private readonly IHttpClientFactory _httpClientFactory;
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    public AuthMessageHandler(
        TokenManager tokenManager,
        OrgManager orgManager,
        AuthenticationStateProvider authStateProvider,
        NavigationManager navigation,
        IHttpClientFactory httpClientFactory
    )
    {
        _tokenManager = tokenManager;
        _orgManager = orgManager;
        _authStateProvider = authStateProvider;
        _navigation = navigation;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken ct
    )
    {
        ct.ThrowIfCancellationRequested();

        var token = await _tokenManager.GetAccessTokenAsync();
        var hasAccessToken = !string.IsNullOrEmpty(token);

        // Proactively refresh if access token is about to expire
        if (hasAccessToken && _tokenManager.IsAccessTokenExpiringSoon())
        {
            var refreshed = await TryRefreshTokenAsync(ct);
            if (refreshed)
                token = await _tokenManager.GetAccessTokenAsync();
        }

        if (hasAccessToken)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var orgSlug = await _orgManager.GetActiveSlugAsync();
        if (!string.IsNullOrEmpty(orgSlug))
            request.Headers.Add("X-Tenant-ID", orgSlug);

        var response = await base.SendAsync(request, ct);

        // If 401 and we have a refresh token, try to refresh and retry once
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && hasAccessToken)
        {
            var renewed = await TryRefreshTokenAsync(ct);
            if (renewed)
            {
                var newToken = await _tokenManager.GetAccessTokenAsync();
                if (!string.IsNullOrEmpty(newToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue(
                        "Bearer",
                        newToken
                    );
                    response = await base.SendAsync(request, ct);
                }
            }
            else
            {
                await _tokenManager.ClearAllAsync();
                if (_authStateProvider is JwtAuthenticationStateProvider jwtProvider)
                    await jwtProvider.NotifyAuthenticationStateChanged();

                // Guard: don't redirect if already on login page
                if (!_navigation.Uri.Contains("/login"))
                {
                    var returnUrl = Uri.EscapeDataString(_navigation.Uri);
                    _navigation.NavigateTo($"/login?returnUrl={returnUrl}", forceLoad: true);
                }
            }
        }

        return response;
    }

    private async Task<bool> TryRefreshTokenAsync(CancellationToken ct)
    {
        await RefreshLock.WaitAsync(ct);
        try
        {
            // Double-check: another thread may have already refreshed
            if (!_tokenManager.IsAccessTokenExpiringSoon())
                return true;

            var refreshToken = await _tokenManager.GetRefreshTokenAsync();
            if (string.IsNullOrEmpty(refreshToken))
                return false;

            var renewClient = _httpClientFactory.CreateClient("Renew");
            var renewResponse = await renewClient.PostAsJsonAsync(
                ApiEndpoints.AuthPaths.Refresh,
                new { refreshToken },
                ct
            );

            if (!renewResponse.IsSuccessStatusCode)
                return false;

            var result = await renewResponse.Content.ReadFromJsonAsync<RefreshResponse>(
                cancellationToken: ct
            );
            if (result is null)
                return false;

            await _tokenManager.SetAccessTokenAsync(result.AccessToken, result.ExpiresAt);
            await _tokenManager.SetRefreshTokenAsync(result.RefreshToken);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private record RefreshResponse(string AccessToken, DateTime ExpiresAt, string RefreshToken);
}

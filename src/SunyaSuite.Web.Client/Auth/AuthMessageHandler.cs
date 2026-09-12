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
    private static readonly string RetriedHeader = "X-Auth-Retried";

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

        // If no access token but we have a refresh token, try refresh before sending
        if (!hasAccessToken)
        {
            var hasRefresh = !string.IsNullOrEmpty(await _tokenManager.GetRefreshTokenAsync());
            if (hasRefresh)
            {
                var refreshed = await TryRefreshTokenAsync(ct);
                if (refreshed)
                {
                    token = await _tokenManager.GetAccessTokenAsync();
                    hasAccessToken = !string.IsNullOrEmpty(token);
                }
            }
        }
        // Proactively refresh if access token is about to expire
        else if (_tokenManager.IsAccessTokenExpiringSoon())
        {
            var refreshed = await TryRefreshTokenAsync(ct);
            if (refreshed)
                token = await _tokenManager.GetAccessTokenAsync();
        }

        if (hasAccessToken && !string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var orgSlug = await _orgManager.GetActiveSlugAsync();
        if (!string.IsNullOrEmpty(orgSlug))
            request.Headers.Add("X-Tenant-ID", orgSlug);

        var response = await base.SendAsync(request, ct);

        // If 401, try refresh and retry once (unless already retried)
        if (
            response.StatusCode == System.Net.HttpStatusCode.Unauthorized
            && !request.Headers.Contains(RetriedHeader)
        )
        {
            response.Dispose();
            var renewed = await TryRefreshTokenAsync(ct);
            if (renewed)
            {
                var newToken = await _tokenManager.GetAccessTokenAsync();
                if (!string.IsNullOrEmpty(newToken))
                {
                    var retryRequest = await CloneRequestAsync(request);
                    retryRequest.Headers.Add(RetriedHeader, "1");
                    response = await base.SendAsync(retryRequest, ct);
                }
            }
        }

        // If still no access token or refresh failed, redirect to login
        if (
            response.StatusCode == System.Net.HttpStatusCode.Unauthorized
            && !_navigation.Uri.Contains("/login")
        )
        {
            response.Dispose();
            await _tokenManager.ClearAllAsync();
            if (_authStateProvider is JwtAuthenticationStateProvider jwtProvider)
                await jwtProvider.NotifyAuthenticationStateChanged();

            var returnUrl = Uri.EscapeDataString(_navigation.Uri);
            _navigation.NavigateTo($"/login?returnUrl={returnUrl}", forceLoad: true);

            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
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
            using var renewResponse = await renewClient.PostAsJsonAsync(
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

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (request.Content is not null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(contentBytes);

            if (request.Content.Headers.ContentType is not null)
                clone.Content.Headers.ContentType = request.Content.Headers.ContentType;
        }

        return clone;
    }

    private record RefreshResponse(string AccessToken, DateTime ExpiresAt, string RefreshToken);
}

using Microsoft.JSInterop;

namespace SunyaSuite.Web.Client.Auth;

public class TokenManager
{
    private const string AccessTokenKey = "auth_access_token";
    private const string AccessTokenExpiryKey = "auth_access_expiry";
    private const string RefreshTokenKey = "auth_refresh_token";
    private readonly IJSRuntime _js;

    private string? _inMemoryToken;
    private DateTime _inMemoryExpiry = DateTime.MinValue;

    public TokenManager(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        if (_inMemoryToken is not null && DateTime.UtcNow < _inMemoryExpiry)
            return _inMemoryToken;

        // Try session storage fallback (for page refresh recovery)
        var stored = await _js.InvokeAsync<string?>("sessionStorage.getItem", AccessTokenKey);
        if (!string.IsNullOrEmpty(stored))
        {
            var expiryStr = await _js.InvokeAsync<string?>(
                "sessionStorage.getItem",
                AccessTokenExpiryKey
            );
            if (
                !string.IsNullOrEmpty(expiryStr)
                && DateTime.TryParse(
                    expiryStr,
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out var expiry
                )
                && DateTime.UtcNow < expiry
            )
            {
                _inMemoryToken = stored;
                _inMemoryExpiry = expiry;
                return _inMemoryToken;
            }
        }

        await ClearAccessTokenAsync();
        return null;
    }

    public bool IsAccessTokenExpiringSoon() => DateTime.UtcNow >= _inMemoryExpiry.AddSeconds(-30);

    public async Task SetAccessTokenAsync(string token, DateTime expiresAt)
    {
        _inMemoryToken = token;
        _inMemoryExpiry = expiresAt;
        // Also persist to sessionStorage for recovery across refresh
        await _js.InvokeVoidAsync("sessionStorage.setItem", AccessTokenKey, token);
        await _js.InvokeVoidAsync(
            "sessionStorage.setItem",
            AccessTokenExpiryKey,
            expiresAt.ToString("O")
        );
    }

    public async Task ClearAccessTokenAsync()
    {
        _inMemoryToken = null;
        _inMemoryExpiry = DateTime.MinValue;
        await _js.InvokeVoidAsync("sessionStorage.removeItem", AccessTokenKey);
        await _js.InvokeVoidAsync("sessionStorage.removeItem", AccessTokenExpiryKey);
    }

    public async Task<string?> GetRefreshTokenAsync() =>
        await _js.InvokeAsync<string?>("sessionStorage.getItem", RefreshTokenKey);

    public async Task SetRefreshTokenAsync(string token) =>
        await _js.InvokeVoidAsync("sessionStorage.setItem", RefreshTokenKey, token);

    public async Task ClearRefreshTokenAsync() =>
        await _js.InvokeVoidAsync("sessionStorage.removeItem", RefreshTokenKey);

    public async Task ClearAllAsync()
    {
        await ClearAccessTokenAsync();
        await ClearRefreshTokenAsync();
    }
}

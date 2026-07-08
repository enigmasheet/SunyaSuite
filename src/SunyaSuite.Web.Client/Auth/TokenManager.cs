using Microsoft.JSInterop;

namespace SunyaSuite.Web.Client.Auth;

public class TokenManager
{
    private const string TokenKey = "auth_token";
    private const string ExpiryKey = "auth_token_expiry";
    private readonly IJSRuntime _js;

    public TokenManager(IJSRuntime js)
    {
        _js = js;
    }

    public async Task<string?> GetTokenAsync()
    {
        if (await IsTokenExpiredAsync())
        {
            await ClearTokenAsync();
            return null;
        }

        return await _js.InvokeAsync<string?>("localStorage.getItem", TokenKey);
    }

    public async Task<bool> IsTokenExpiredAsync()
    {
        var expiryStr = await _js.InvokeAsync<string?>("localStorage.getItem", ExpiryKey);
        if (string.IsNullOrEmpty(expiryStr))
            return true;

        if (DateTime.TryParse(expiryStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expiry))
            return DateTime.UtcNow >= expiry;

        return true;
    }

    public async Task SetTokenAsync(string token, DateTime expiresAt)
    {
        await _js.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
        await _js.InvokeVoidAsync("localStorage.setItem", ExpiryKey, expiresAt.ToString("O"));
    }

    public async Task RenewTokenAsync(string token, DateTime expiresAt)
    {
        await SetTokenAsync(token, expiresAt);
    }

    public async Task ClearTokenAsync()
    {
        await _js.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        await _js.InvokeVoidAsync("localStorage.removeItem", ExpiryKey);
    }
}

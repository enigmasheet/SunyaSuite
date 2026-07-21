using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Text.Json;

namespace SunyaSuite.Web.Client.Auth;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly TokenManager _tokenManager;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public JwtAuthenticationStateProvider(TokenManager tokenManager)
    {
        _tokenManager = tokenManager;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _tokenManager.GetTokenAsync();

        if (string.IsNullOrEmpty(token))
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var claims = ParseClaimsFromJwt(token);
        var identity = new ClaimsIdentity(claims, "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public async Task NotifyAuthenticationStateChanged()
    {
        var state = await GetAuthenticationStateAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(state));
    }

    private static IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes, JsonOptions);

        var claims = new List<Claim>();
        if (keyValuePairs is null) return claims;

        foreach (var (key, value) in keyValuePairs)
        {
            switch (key)
            {
                case "sub":
                case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier":
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, value.GetString() ?? ""));
                    break;

                case "email":
                case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress":
                    claims.Add(new Claim(ClaimTypes.Email, value.GetString() ?? ""));
                    claims.Add(new Claim(ClaimTypes.Name, value.GetString() ?? ""));
                    break;

                case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name":
                    claims.Add(new Claim(ClaimTypes.Name, value.GetString() ?? ""));
                    break;

                case "role":
                case "roles":
                case "http://schemas.microsoft.com/ws/2008/06/identity/claims/role":
                case "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role":
                    if (value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var role in value.EnumerateArray())
                            claims.Add(new Claim(ClaimTypes.Role, role.GetString() ?? ""));
                    }
                    else
                    {
                        claims.Add(new Claim(ClaimTypes.Role, value.GetString() ?? ""));
                    }
                    break;

                case "org_role":
                    if (value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var role in value.EnumerateArray())
                            claims.Add(new Claim("org_role", role.GetString() ?? ""));
                    }
                    else
                    {
                        claims.Add(new Claim("org_role", value.GetString() ?? ""));
                    }
                    break;

                case "exp":
                case "iss":
                case "aud":
                case "iat":
                case "nbf":
                    break;

                default:
                    var strValue = value.ValueKind switch
                    {
                        JsonValueKind.String => value.GetString() ?? "",
                        JsonValueKind.Number => value.GetRawText(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        _ => value.GetRawText()
                    };
                    claims.Add(new Claim(key, strValue));
                    break;
            }
        }

        return claims;
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}

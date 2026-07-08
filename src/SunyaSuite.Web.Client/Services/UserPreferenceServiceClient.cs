using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Domain.Enums;
using System.Net.Http.Json;

namespace SunyaSuite.Web.Client.Services;

public class UserPreferenceServiceClient : IUserPreferenceService
{
    private readonly HttpClient _http;

    public UserPreferenceServiceClient(HttpClient http) => _http = http;

    public async Task<DateDisplayPreference> GetDateDisplayPreferenceAsync(string userId, CancellationToken ct = default)
    {
        var result = await _http.GetFromJsonAsync<DateDisplayPreference?>($"{ApiEndpoints.UserPreferences}/{userId}", ct);
        return result ?? DateDisplayPreference.Gregorian;
    }

    public async Task SetDateDisplayPreferenceAsync(string userId, DateDisplayPreference preference, CancellationToken ct = default) =>
        (await _http.PostAsJsonAsync($"{ApiEndpoints.UserPreferences}/{userId}", new { preference }, ct)).EnsureSuccessStatusCode();
}

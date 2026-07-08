using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Domain.Entities.Tenant;
using System.Net.Http.Json;

namespace SunyaSuite.Web.Client.Services;

public class NotificationPreferenceServiceClient : INotificationPreferenceService
{
    private readonly HttpClient _http;

    public NotificationPreferenceServiceClient(HttpClient http) => _http = http;

    public async Task<List<NotificationPreference>> GetForUserAsync(string userId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<NotificationPreference>>($"{ApiEndpoints.NotificationPreferences}/{userId}", ct) ?? [];

    public async Task ToggleAsync(string userId, string type, bool enabled, CancellationToken ct = default) =>
        (await _http.PostAsJsonAsync($"{ApiEndpoints.NotificationPreferences}/{userId}/toggle", new { type, enabled }, ct)).EnsureSuccessStatusCode();

    public async Task SeedDefaultsAsync(string userId, CancellationToken ct = default) =>
        (await _http.PostAsync($"{ApiEndpoints.NotificationPreferences}/{userId}/seed", null, ct)).EnsureSuccessStatusCode();
}

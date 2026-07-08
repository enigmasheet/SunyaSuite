using SunyaSuite.Application.DTOs.Config;
using SunyaSuite.Application.Interfaces.Config;
using System.Net.Http.Json;

namespace SunyaSuite.Web.Client.Services;

public class SystemDashboardServiceClient : ISystemDashboardService
{
    private readonly HttpClient _http;

    public SystemDashboardServiceClient(HttpClient http) => _http = http;

    public async Task<SystemDashboardStats> GetStatsAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<SystemDashboardStats>("api/admin/dashboard", ct)
        ?? new SystemDashboardStats();
}

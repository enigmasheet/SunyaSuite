using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Tenant;
using System.Net.Http.Json;

namespace SunyaSuite.Web.Client.Services;

public class DashboardServiceClient : IDashboardService
{
    private readonly HttpClient _http;

    public DashboardServiceClient(HttpClient http) => _http = http;

    public async Task<DashboardStats> GetStatsAsync(Guid? fiscalYearId = null, CancellationToken ct = default)
    {
        var query = ApiEndpoints.Dashboard;
        if (fiscalYearId.HasValue) query += $"?fiscalYearId={fiscalYearId.Value}";
        return await _http.GetFromJsonAsync<DashboardStats>(query, ct) ?? new();
    }

    public async Task<List<RecentInvoiceDto>> GetRecentInvoicesAsync(int count = 5, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<RecentInvoiceDto>>($"{ApiEndpoints.Dashboard}/recent?count={count}", ct) ?? [];
}

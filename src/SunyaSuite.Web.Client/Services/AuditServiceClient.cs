using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.Interfaces;
using System.Net.Http.Json;

namespace SunyaSuite.Web.Client.Services;

public class AuditServiceClient : IAuditService
{
    private readonly HttpClient _http;

    public AuditServiceClient(HttpClient http) => _http = http;

    public async Task LogAsync(string userId, string action, string entityName, string entityId, string? details = null, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(ApiEndpoints.Audit, new { userId, action, entityName, entityId, details }, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<PagedResult<AuditLogDto>> GetRecentAsync(int page = 1, int pageSize = 50, AuditLogFilterDto? filter = null, CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                queryParams.Add($"searchTerm={Uri.EscapeDataString(filter.SearchTerm)}");
            if (!string.IsNullOrWhiteSpace(filter.Action))
                queryParams.Add($"action={Uri.EscapeDataString(filter.Action)}");
            if (!string.IsNullOrWhiteSpace(filter.EntityName))
                queryParams.Add($"entityName={Uri.EscapeDataString(filter.EntityName)}");
            if (filter.DateFrom.HasValue)
                queryParams.Add($"dateFrom={filter.DateFrom.Value:O}");
            if (filter.DateTo.HasValue)
                queryParams.Add($"dateTo={filter.DateTo.Value:O}");
        }

        var url = $"{ApiEndpoints.Audit}?{string.Join("&", queryParams)}";
        return await _http.GetFromJsonAsync<PagedResult<AuditLogDto>>(url, ct) ?? new([], 0);
    }

    public async Task<List<string>> GetDistinctActionsAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<string>>($"{ApiEndpoints.Audit}/actions", ct) ?? [];

    public async Task<List<string>> GetDistinctEntityNamesAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<string>>($"{ApiEndpoints.Audit}/entity-names", ct) ?? [];
}

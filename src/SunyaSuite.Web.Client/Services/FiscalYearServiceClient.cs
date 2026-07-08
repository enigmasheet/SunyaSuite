using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Tenant;
using System.Net.Http.Json;

namespace SunyaSuite.Web.Client.Services;

public class FiscalYearServiceClient : IFiscalYearService
{
    private readonly HttpClient _http;

    public FiscalYearServiceClient(HttpClient http) => _http = http;

    public async Task<FiscalYearDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<FiscalYearDto>($"{ApiEndpoints.FiscalYears}/{id}", ct);

    public async Task<FiscalYearDto?> GetCurrentAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<FiscalYearDto>($"{ApiEndpoints.FiscalYears}/current", ct);

    public async Task<List<FiscalYearListItemDto>> GetAllAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<FiscalYearListItemDto>>(ApiEndpoints.FiscalYears, ct) ?? [];

    public async Task<List<FiscalYearListItemDto>> GetOpenYearsAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<FiscalYearListItemDto>>($"{ApiEndpoints.FiscalYears}/open", ct) ?? [];

    public async Task<FiscalYearListItemDto> CreateAsync(CreateFiscalYearRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(ApiEndpoints.FiscalYears, request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FiscalYearListItemDto>(cancellationToken: ct))!;
    }

    public async Task ToggleOpenAsync(Guid id, CancellationToken ct = default) =>
        (await _http.PostAsync($"{ApiEndpoints.FiscalYears}/{id}/toggle-open", null, ct)).EnsureSuccessStatusCode();

    public async Task SetCurrentAsync(Guid id, CancellationToken ct = default) =>
        (await _http.PostAsync($"{ApiEndpoints.FiscalYears}/{id}/set-current", null, ct)).EnsureSuccessStatusCode();
}

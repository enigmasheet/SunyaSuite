using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Tenant;
using System.Net.Http.Json;

namespace SunyaSuite.Web.Client.Services;

public class MoneyReceiptServiceClient : IMoneyReceiptService
{
    private readonly HttpClient _http;

    public MoneyReceiptServiceClient(HttpClient http) => _http = http;

    public async Task<MoneyReceiptDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<MoneyReceiptDetailDto>($"{ApiEndpoints.MoneyReceipts}/{id}", ct);

    public async Task<PagedResult<MoneyReceiptListItemDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, string? sortLabel = null, string? sortDirection = null, Guid? fiscalYearId = null, CancellationToken ct = default)
    {
        var query = $"{ApiEndpoints.MoneyReceipts}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(searchTerm)) query += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
        if (!string.IsNullOrEmpty(sortLabel)) query += $"&sortLabel={sortLabel}";
        if (!string.IsNullOrEmpty(sortDirection)) query += $"&sortDirection={sortDirection}";
        if (fiscalYearId.HasValue) query += $"&fiscalYearId={fiscalYearId.Value}";
        return await _http.GetFromJsonAsync<PagedResult<MoneyReceiptListItemDto>>(query, ct) ?? new([], 0);
    }

    public async Task<MoneyReceiptListItemDto> CreateAsync(CreateMoneyReceiptRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(ApiEndpoints.MoneyReceipts, request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MoneyReceiptListItemDto>(cancellationToken: ct))!;
    }

    public async Task<MoneyReceiptListItemDto> UpdateAsync(UpdateMoneyReceiptRequest request, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"{ApiEndpoints.MoneyReceipts}/{request.Id}", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MoneyReceiptListItemDto>(cancellationToken: ct))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default) =>
        (await _http.DeleteAsync($"{ApiEndpoints.MoneyReceipts}/{id}", ct)).EnsureSuccessStatusCode();

    public async Task RestoreAsync(Guid id, CancellationToken ct = default) =>
        (await _http.PostAsync($"{ApiEndpoints.MoneyReceipts}/{id}/restore", null, ct)).EnsureSuccessStatusCode();

    public async Task<PagedResult<MoneyReceiptListItemDto>> GetDeletedPagedAsync(int page, int pageSize, string? searchTerm = null, CancellationToken ct = default)
    {
        var query = $"{ApiEndpoints.MoneyReceipts}/deleted?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(searchTerm)) query += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
        return await _http.GetFromJsonAsync<PagedResult<MoneyReceiptListItemDto>>(query, ct) ?? new([], 0);
    }

    public async Task PermanentDeleteAsync(Guid id, CancellationToken ct = default) =>
        (await _http.DeleteAsync($"{ApiEndpoints.MoneyReceipts}/{id}/permanent", ct)).EnsureSuccessStatusCode();
}

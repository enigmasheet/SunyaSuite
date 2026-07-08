using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Enums;
using System.Net.Http.Json;

namespace SunyaSuite.Web.Client.Services;

public class InvoiceServiceClient : IInvoiceService
{
    private readonly HttpClient _http;

    public InvoiceServiceClient(HttpClient http) => _http = http;

    public async Task<InvoiceDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<InvoiceDetailDto>($"{ApiEndpoints.Invoices}/{id}", ct);

    public async Task<PagedResult<InvoiceListItemDto>> GetPagedAsync(int page, int pageSize, string? sortLabel, string? sortDirection, string? searchTerm = null, InvoiceFilterDto? filter = null, CancellationToken ct = default)
    {
        var query = $"{ApiEndpoints.Invoices}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(sortLabel)) query += $"&sortLabel={sortLabel}";
        if (!string.IsNullOrEmpty(sortDirection)) query += $"&sortDirection={sortDirection}";
        if (!string.IsNullOrEmpty(searchTerm)) query += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
        return await _http.GetFromJsonAsync<PagedResult<InvoiceListItemDto>>(query, ct) ?? new([], 0);
    }

    public async Task<InvoiceListItemDto> CreateAsync(CreateInvoiceRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(ApiEndpoints.Invoices, request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<InvoiceListItemDto>(cancellationToken: ct))!;
    }

    public async Task<InvoiceListItemDto> UpdateAsync(UpdateInvoiceRequest request, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"{ApiEndpoints.Invoices}/{request.Id}", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<InvoiceListItemDto>(cancellationToken: ct))!;
    }

    public async Task<List<InvoiceSelectionDto>> GetInvoiceSelectionAsync(Guid? fiscalYearId = null, CancellationToken ct = default)
    {
        var query = $"{ApiEndpoints.Invoices}/selection";
        if (fiscalYearId.HasValue) query += $"?fiscalYearId={fiscalYearId.Value}";
        return await _http.GetFromJsonAsync<List<InvoiceSelectionDto>>(query, ct) ?? [];
    }

    public async Task UpdateStatusAsync(Guid id, InvoiceStatus status, CancellationToken ct = default) =>
        (await _http.PatchAsJsonAsync($"{ApiEndpoints.Invoices}/{id}/status", new { status }, ct)).EnsureSuccessStatusCode();

    public async Task DeleteAsync(Guid id, CancellationToken ct = default) =>
        (await _http.DeleteAsync($"{ApiEndpoints.Invoices}/{id}", ct)).EnsureSuccessStatusCode();

    public async Task<PagedResult<DeletedInvoiceDto>> GetDeletedPagedAsync(int page, int pageSize, string? searchTerm = null, CancellationToken ct = default)
    {
        var query = $"{ApiEndpoints.Invoices}/deleted?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(searchTerm)) query += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
        return await _http.GetFromJsonAsync<PagedResult<DeletedInvoiceDto>>(query, ct) ?? new([], 0);
    }

    public async Task RestoreAsync(Guid id, CancellationToken ct = default) =>
        (await _http.PostAsync($"{ApiEndpoints.Invoices}/{id}/restore", null, ct)).EnsureSuccessStatusCode();

    public async Task PermanentDeleteAsync(Guid id, CancellationToken ct = default) =>
        (await _http.DeleteAsync($"{ApiEndpoints.Invoices}/{id}/permanent", ct)).EnsureSuccessStatusCode();
}

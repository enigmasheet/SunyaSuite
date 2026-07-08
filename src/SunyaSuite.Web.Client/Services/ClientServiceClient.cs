using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Tenant;
using System.Net.Http.Json;

namespace SunyaSuite.Web.Client.Services;

public class ClientServiceClient : IClientService
{
    private readonly HttpClient _http;

    public ClientServiceClient(HttpClient http) => _http = http;

    public async Task<List<ClientOptionDto>> GetClientOptionsAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<ClientOptionDto>>($"{ApiEndpoints.Clients}/options", ct) ?? [];

    public async Task<ClientDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<ClientDetailDto>($"{ApiEndpoints.Clients}/{id}", ct);

    public async Task<PagedResult<ClientListItemDto>> GetPagedAsync(int page, int pageSize, string? sortLabel, string? sortDirection, string? searchTerm = null, ClientFilterDto? filter = null, CancellationToken ct = default)
    {
        var query = $"{ApiEndpoints.Clients}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(sortLabel)) query += $"&sortLabel={sortLabel}";
        if (!string.IsNullOrEmpty(sortDirection)) query += $"&sortDirection={sortDirection}";
        if (!string.IsNullOrEmpty(searchTerm)) query += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
        return await _http.GetFromJsonAsync<PagedResult<ClientListItemDto>>(query, ct) ?? new([], 0);
    }

    public async Task<ClientListItemDto> CreateAsync(CreateClientRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(ApiEndpoints.Clients, request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClientListItemDto>(cancellationToken: ct))!;
    }

    public async Task<ClientListItemDto> UpdateAsync(UpdateClientRequest request, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"{ApiEndpoints.Clients}/{request.Id}", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClientListItemDto>(cancellationToken: ct))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default) =>
        (await _http.DeleteAsync($"{ApiEndpoints.Clients}/{id}", ct)).EnsureSuccessStatusCode();

    public async Task<PagedResult<DeletedClientDto>> GetDeletedPagedAsync(int page, int pageSize, string? searchTerm = null, CancellationToken ct = default)
    {
        var query = $"{ApiEndpoints.Clients}/deleted?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(searchTerm)) query += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
        return await _http.GetFromJsonAsync<PagedResult<DeletedClientDto>>(query, ct) ?? new([], 0);
    }

    public async Task RestoreAsync(Guid id, CancellationToken ct = default) =>
        (await _http.PostAsync($"{ApiEndpoints.Clients}/{id}/restore", null, ct)).EnsureSuccessStatusCode();

    public async Task PermanentDeleteAsync(Guid id, CancellationToken ct = default) =>
        (await _http.DeleteAsync($"{ApiEndpoints.Clients}/{id}/permanent", ct)).EnsureSuccessStatusCode();
}

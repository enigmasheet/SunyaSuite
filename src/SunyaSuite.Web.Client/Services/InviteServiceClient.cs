using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.DTOs.Config;
using SunyaSuite.Application.Interfaces.Config;
using System.Net.Http.Json;

namespace SunyaSuite.Web.Client.Services;

public class InviteServiceClient : IInviteService
{
    private readonly HttpClient _http;

    public InviteServiceClient(HttpClient http) => _http = http;

    public async Task<(List<InviteDto> Items, int Total)> GetPagedAsync(Guid organizationId, int page, int pageSize, string? searchTerm = null, CancellationToken ct = default)
    {
        var query = $"{ApiEndpoints.Invites}?organizationId={organizationId}&page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(searchTerm)) query += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
        var result = await _http.GetFromJsonAsync<PagedResult<InviteDto>>(query, ct) ?? new([], 0);
        return (result.Items, result.Total);
    }

    public async Task<InviteDto> CreateAsync(Guid organizationId, string role, int? expiresInHours, string createdByUserId, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(ApiEndpoints.Invites, new { organizationId, role, expiresInHours }, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<InviteDto>(cancellationToken: ct))!;
    }

    public Task<InviteDto?> GetByCodeAsync(string code, CancellationToken ct = default) =>
        throw new NotSupportedException("GetByCodeAsync is a server-side operation; use ValidateInviteAsync instead.");

    public Task<bool> ValidateInviteAsync(string code, CancellationToken ct = default) =>
        throw new NotSupportedException("ValidateInviteAsync is a server-side operation called from AuthController.");

    public Task<(string Role, string Code, Guid OrganizationId)> ConsumeInviteAsync(string code, string usedByEmail, CancellationToken ct = default) =>
        throw new NotSupportedException("ConsumeInviteAsync is a server-side operation called from AuthController.");

    public async Task DeleteAsync(Guid id, CancellationToken ct = default) =>
        (await _http.DeleteAsync($"{ApiEndpoints.Invites}/{id}", ct)).EnsureSuccessStatusCode();
}

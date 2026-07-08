using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.DTOs.Config;
using SunyaSuite.Application.Interfaces.Config;
using System.Net.Http.Json;

namespace SunyaSuite.Web.Client.Services;

public class OrganizationServiceClient : IOrganizationService
{
    private readonly HttpClient _http;

    public OrganizationServiceClient(HttpClient http) => _http = http;

    public async Task<List<OrganizationDto>> GetMyOrganizationsAsync(string userId) =>
        await _http.GetFromJsonAsync<List<OrganizationDto>>($"{ApiEndpoints.Organizations}/my") ?? [];

    public async Task<List<OrganizationDto>> GetAllAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<OrganizationDto>>(ApiEndpoints.Organizations, ct) ?? [];

    public async Task<(List<OrganizationDto> Items, int Total)> GetPagedAsync(int page, int pageSize, string? searchTerm = null, CancellationToken ct = default)
    {
        var query = $"{ApiEndpoints.Organizations}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(searchTerm)) query += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
        var result = await _http.GetFromJsonAsync<PagedResult<OrganizationDto>>(query, ct) ?? new([], 0);
        return (result.Items, result.Total);
    }

    public async Task<OrganizationDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<OrganizationDto>($"{ApiEndpoints.Organizations}/{id}", ct);

    public async Task<OrganizationDto> CreateAsync(CreateOrganizationRequest request, string adminUserId)
    {
        var response = await _http.PostAsJsonAsync(ApiEndpoints.Organizations, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrganizationDto>())!;
    }

    public Task<List<OrganizationUserDto>> GetUserOrganizationsAsync(string userId, CancellationToken ct = default) =>
        throw new NotSupportedException("Use GetOrgUsersAsync from client side.");

    public Task AssignToOrganizationAsync(string userId, Guid organizationId, string role, CancellationToken ct = default) =>
        throw new NotSupportedException("User assignment is only supported server-side.");

    public Task UpdateOrganizationRoleAsync(string userId, Guid organizationId, string role, CancellationToken ct = default) =>
        throw new NotSupportedException("Role update is only supported server-side.");

    public Task RemoveFromOrganizationAsync(string userId, Guid organizationId, CancellationToken ct = default) =>
        throw new NotSupportedException("User removal is only supported server-side.");

    public async Task<List<OrganizationUserDto>> GetOrgUsersAsync(Guid organizationId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<OrganizationUserDto>>($"{ApiEndpoints.Organizations}/{organizationId}/users", ct) ?? [];

    public async Task<UserDto> CreateUserForOrganizationAsync(Guid organizationId, CreateOrgUserRequest request, string adminUserId)
    {
        var response = await _http.PostAsJsonAsync($"{ApiEndpoints.Organizations}/{organizationId}/users", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserDto>())!;
    }

    public async Task<OrganizationDto> UpdateAsync(Guid id, UpdateOrganizationRequest request)
    {
        var response = await _http.PutAsJsonAsync($"{ApiEndpoints.Organizations}/{id}", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrganizationDto>())!;
    }

    public async Task DeleteAsync(Guid id) =>
        (await _http.DeleteAsync($"{ApiEndpoints.Organizations}/{id}")).EnsureSuccessStatusCode();

    public async Task ToggleActiveAsync(Guid id) =>
        (await _http.PatchAsync($"{ApiEndpoints.Organizations}/{id}/toggle-active", null)).EnsureSuccessStatusCode();

    public async Task UpdateOrgUserRoleAsync(Guid orgId, string userId, string role) =>
        (await _http.PutAsJsonAsync($"{ApiEndpoints.Organizations}/{orgId}/users/{userId}/role", new { role })).EnsureSuccessStatusCode();

    public async Task UpdateOrgUserDefaultsAsync(Guid orgId, string userId, Guid? companyId, Guid? branchId) =>
        (await _http.PutAsJsonAsync($"{ApiEndpoints.Organizations}/{orgId}/users/{userId}/defaults",
            new { defaultCompanyId = companyId, defaultBranchId = branchId })).EnsureSuccessStatusCode();
}

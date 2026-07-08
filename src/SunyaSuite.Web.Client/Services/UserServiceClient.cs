using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.DTOs.Config;
using SunyaSuite.Application.Interfaces.Config;
using System.Net.Http.Json;

namespace SunyaSuite.Web.Client.Services;

public class UserServiceClient : IUserService
{
    private readonly HttpClient _http;

    public UserServiceClient(HttpClient http) => _http = http;

    public async Task<(List<UserDto> Items, int Total)> GetPagedAsync(int page, int pageSize, string? searchTerm = null, CancellationToken ct = default)
    {
        var query = $"{ApiEndpoints.Users}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(searchTerm)) query += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
        var result = await _http.GetFromJsonAsync<PagedResult<UserDto>>(query, ct) ?? new([], 0);
        return (result.Items, result.Total);
    }

    public async Task<UserDto?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<UserDto>($"{ApiEndpoints.Users}/{id}", ct);

    public async Task<UserDto> CreateAsync(string email, string password, string firstName, string lastName, List<string> roles, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(ApiEndpoints.Users, new { email, password, firstName, lastName, roles }, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: ct))!;
    }

    public async Task<UserDto> UpdateAsync(string id, string email, string firstName, string lastName, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"{ApiEndpoints.Users}/{id}", new { email, firstName, lastName }, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken: ct))!;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default) =>
        (await _http.DeleteAsync($"{ApiEndpoints.Users}/{id}", ct)).EnsureSuccessStatusCode();

    public async Task AssignRolesAsync(string id, List<string> roles, CancellationToken ct = default) =>
        (await _http.PostAsJsonAsync($"{ApiEndpoints.Users}/{id}/roles", new { roles }, ct)).EnsureSuccessStatusCode();

    public async Task<List<string>> GetAllRolesAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<string>>($"{ApiEndpoints.Users}/roles", ct) ?? [];

    public async Task<List<string>> GetOrgRolesAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<string>>($"{ApiEndpoints.Users}/org-roles", ct) ?? [];

    public async Task CreateRoleAsync(string roleName, CancellationToken ct = default) =>
        (await _http.PostAsJsonAsync($"{ApiEndpoints.Users}/roles", new { roleName }, ct)).EnsureSuccessStatusCode();

    public async Task DeleteRoleAsync(string roleName, CancellationToken ct = default) =>
        (await _http.DeleteAsync($"{ApiEndpoints.Users}/roles/{Uri.EscapeDataString(roleName)}", ct)).EnsureSuccessStatusCode();
}

using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Tenant;
using System.Net.Http.Json;

namespace SunyaSuite.Web.Client.Services;

public class ProjectServiceClient(HttpClient http) : IProjectService
{
    public async Task<List<ProjectOptionDto>> GetProjectOptionsAsync(Guid? clientId = null, CancellationToken ct = default)
    {
        var query = $"{ApiEndpoints.Projects}/options";
        if (clientId.HasValue) query += $"?clientId={clientId.Value}";
        return await http.GetFromJsonAsync<List<ProjectOptionDto>>(query, ct) ?? [];
    }

    public async Task<ProjectDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await http.GetFromJsonAsync<ProjectDetailDto>($"{ApiEndpoints.Projects}/{id}", ct);
   
    public async Task<PagedResult<ProjectListItemDto>> GetPagedAsync(int page, int pageSize, string? sortLabel, string? sortDirection, string? searchTerm = null, ProjectFilterDto? filter = null, CancellationToken ct = default)
    {
        var query = $"{ApiEndpoints.Projects}?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(sortLabel)) query += $"&sortLabel={sortLabel}";
        if (!string.IsNullOrEmpty(sortDirection)) query += $"&sortDirection={sortDirection}";
        if (!string.IsNullOrEmpty(searchTerm)) query += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
        return await http.GetFromJsonAsync<PagedResult<ProjectListItemDto>>(query, ct) ?? new([], 0);
    }

    public async Task<ProjectListItemDto> CreateAsync(CreateProjectRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync(ApiEndpoints.Projects, request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProjectListItemDto>(cancellationToken: ct))!;
    }

    public async Task<ProjectListItemDto> UpdateAsync(UpdateProjectRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"{ApiEndpoints.Projects}/{request.Id}", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProjectListItemDto>(cancellationToken: ct))!;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default) =>
        (await http.DeleteAsync($"{ApiEndpoints.Projects}/{id}", ct)).EnsureSuccessStatusCode();

    public async Task<PagedResult<DeletedProjectDto>> GetDeletedPagedAsync(int page, int pageSize, string? searchTerm = null, CancellationToken ct = default)
    {
        var query = $"{ApiEndpoints.Projects}/deleted?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(searchTerm)) query += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
        return await http.GetFromJsonAsync<PagedResult<DeletedProjectDto>>(query, ct) ?? new([], 0);
    }

    public async Task RestoreAsync(Guid id, CancellationToken ct = default) =>
        (await http.PostAsync($"{ApiEndpoints.Projects}/{id}/restore", null, ct)).EnsureSuccessStatusCode();

    public async Task PermanentDeleteAsync(Guid id, CancellationToken ct = default) =>
        (await http.DeleteAsync($"{ApiEndpoints.Projects}/{id}/permanent", ct)).EnsureSuccessStatusCode();
}

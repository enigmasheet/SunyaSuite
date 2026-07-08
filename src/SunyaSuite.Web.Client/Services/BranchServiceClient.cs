using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Tenant;
using System.Net.Http.Json;

namespace SunyaSuite.Web.Client.Services;

public class BranchServiceClient : IBranchService
{
    private readonly HttpClient _http;

    public BranchServiceClient(HttpClient http) => _http = http;

    public async Task<BranchDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<BranchDto>($"{ApiEndpoints.Branches}/{id}", ct);

    public async Task<List<BranchDto>> GetAllAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<BranchDto>>(ApiEndpoints.Branches, ct) ?? [];

    public async Task<List<BranchDto>> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<BranchDto>>($"{ApiEndpoints.Branches}?companyId={companyId}", ct) ?? [];

    public async Task<List<BranchDto>> GetDeletedAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<BranchDto>>($"{ApiEndpoints.Branches}/deleted", ct) ?? [];

    public async Task<BranchDto> CreateAsync(CreateBranchRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(ApiEndpoints.Branches, request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BranchDto>(cancellationToken: ct))!;
    }

    public async Task<BranchDto> UpdateAsync(UpdateBranchRequest request, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"{ApiEndpoints.Branches}/{request.Id}", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BranchDto>(cancellationToken: ct))!;
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct = default) =>
        (await _http.DeleteAsync($"{ApiEndpoints.Branches}/{id}", ct)).EnsureSuccessStatusCode();

    public async Task RestoreAsync(Guid id, CancellationToken ct = default) =>
        (await _http.PatchAsync($"{ApiEndpoints.Branches}/{id}/restore", null, ct)).EnsureSuccessStatusCode();
}

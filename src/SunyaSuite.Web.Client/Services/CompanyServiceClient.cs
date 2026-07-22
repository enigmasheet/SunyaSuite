using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Tenant;
using System.Net.Http.Json;

namespace SunyaSuite.Web.Client.Services;

public class CompanyServiceClient : ICompanyService
{
    private readonly HttpClient _http;

    public CompanyServiceClient(HttpClient http) => _http = http;

    public async Task<CompanyDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<CompanyDto>($"{ApiEndpoints.Companies}/{id}", ct);

    public async Task<List<CompanyDto>> GetAllAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<CompanyDto>>(ApiEndpoints.Companies, ct) ?? [];

    public async Task<List<CompanyDto>> GetActiveAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<CompanyDto>>($"{ApiEndpoints.Companies}/active", ct) ?? [];

    public async Task<List<CompanyDto>> GetDeletedAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<List<CompanyDto>>($"{ApiEndpoints.Companies}/deleted", ct) ?? [];

    public async Task<CompanyDto> CreateAsync(CreateCompanyRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(ApiEndpoints.Companies, request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CompanyDto>(cancellationToken: ct))!;
    }

    public async Task<CompanyDto> UpdateAsync(UpdateCompanyRequest request, CancellationToken ct = default)
    {
        var response = await _http.PutAsJsonAsync($"{ApiEndpoints.Companies}/{request.Id}", request, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CompanyDto>(cancellationToken: ct))!;
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct = default) =>
        (await _http.DeleteAsync($"{ApiEndpoints.Companies}/{id}", ct)).EnsureSuccessStatusCode();

    public async Task RestoreAsync(Guid id, CancellationToken ct = default) =>
        (await _http.PostAsync($"{ApiEndpoints.Companies}/{id}/restore", null, ct)).EnsureSuccessStatusCode();

    public async Task ToggleActiveAsync(Guid id, CancellationToken ct = default) =>
        (await _http.PostAsync($"{ApiEndpoints.Companies}/{id}/toggle-active", null, ct)).EnsureSuccessStatusCode();
}

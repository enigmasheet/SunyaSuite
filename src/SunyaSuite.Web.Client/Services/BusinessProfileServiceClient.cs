using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.Interfaces;
using System.Net.Http.Json;

namespace SunyaSuite.Web.Client.Services;

public class BusinessProfileServiceClient : IBusinessProfileService
{
    private readonly HttpClient _http;

    public BusinessProfileServiceClient(HttpClient http) => _http = http;

    public async Task<BusinessProfileDto?> GetDefaultAsync(CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<BusinessProfileDto>($"{ApiEndpoints.BusinessProfile}/default", ct);

    public async Task<BusinessProfileDto?> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default) =>
        await _http.GetFromJsonAsync<BusinessProfileDto>($"{ApiEndpoints.BusinessProfile}/{companyId}", ct);

    public async Task SaveDefaultAsync(BusinessProfileDto dto, CancellationToken ct = default) =>
        (await _http.PostAsJsonAsync(ApiEndpoints.BusinessProfile, dto, ct)).EnsureSuccessStatusCode();

    public async Task SaveAsync(Guid companyId, BusinessProfileDto dto, CancellationToken ct = default) =>
        (await _http.PutAsJsonAsync($"{ApiEndpoints.BusinessProfile}/{companyId}", dto, ct)).EnsureSuccessStatusCode();
}

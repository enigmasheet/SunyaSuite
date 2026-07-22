using Microsoft.JSInterop;
using SunyaSuite.Application.DTOs.Config;
using System.Text.Json;

namespace SunyaSuite.Web.Client.Auth;

public class OrgManager
{
    private const string OrgsKey = "sunya_orgs";
    private const string ActiveSlugKey = "sunya_active_org";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IJSRuntime _js;
    private List<OrganizationDto>? _cachedOrgs;
    private string? _cachedActiveSlug;

    public OrgManager(IJSRuntime js)
    {
        _js = js;
    }

    public async Task SetOrganizationsAsync(List<OrganizationDto> orgs)
    {
        _cachedOrgs = orgs;
        var json = JsonSerializer.Serialize(orgs);
        await _js.InvokeVoidAsync("sessionStorage.setItem", OrgsKey, json);
    }

    public async Task<List<OrganizationDto>> GetOrganizationsAsync()
    {
        if (_cachedOrgs is not null)
            return _cachedOrgs;

        var json = await _js.InvokeAsync<string?>("sessionStorage.getItem", OrgsKey);
        if (string.IsNullOrEmpty(json))
            return [];

        _cachedOrgs = JsonSerializer.Deserialize<List<OrganizationDto>>(json, JsonOptions) ?? [];
        return _cachedOrgs;
    }

    public async Task<string?> GetActiveSlugAsync()
    {
        if (_cachedActiveSlug is not null)
            return _cachedActiveSlug;

        _cachedActiveSlug = await _js.InvokeAsync<string?>("sessionStorage.getItem", ActiveSlugKey);
        return _cachedActiveSlug;
    }

    public async Task SetActiveSlugAsync(string slug)
    {
        _cachedActiveSlug = slug;
        await _js.InvokeVoidAsync("sessionStorage.setItem", ActiveSlugKey, slug);
    }

    public async Task<OrganizationDto?> GetActiveOrgAsync()
    {
        var slug = await GetActiveSlugAsync();
        if (string.IsNullOrEmpty(slug))
            return null;

        var orgs = await GetOrganizationsAsync();
        return orgs.FirstOrDefault(o => o.Slug == slug);
    }

    public async Task ClearAsync()
    {
        _cachedOrgs = null;
        _cachedActiveSlug = null;
        await _js.InvokeVoidAsync("sessionStorage.removeItem", OrgsKey);
        await _js.InvokeVoidAsync("sessionStorage.removeItem", ActiveSlugKey);
    }
}

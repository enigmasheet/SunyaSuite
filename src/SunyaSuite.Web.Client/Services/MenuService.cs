using SunyaSuite.Application.DTOs.Config;
using System.Net.Http.Json;

namespace SunyaSuite.Web.Client.Services;

public interface IMenuService
{
    Task<List<MenuSectionDto>> GetMenuAsync();
    void InvalidateCache();
}

public class MenuService : IMenuService
{
    private readonly HttpClient _http;
    private List<MenuSectionDto>? _cached;
    private DateTime _cacheTimestamp;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public MenuService(HttpClient http) => _http = http;

    public async Task<List<MenuSectionDto>> GetMenuAsync()
    {
        if (_cached is not null && DateTime.UtcNow - _cacheTimestamp < CacheDuration)
            return _cached;

        _cached = await _http.GetFromJsonAsync<List<MenuSectionDto>>(ApiEndpoints.Menu) ?? [];
        _cacheTimestamp = DateTime.UtcNow;
        return _cached;
    }

    public void InvalidateCache()
    {
        _cached = null;
    }
}

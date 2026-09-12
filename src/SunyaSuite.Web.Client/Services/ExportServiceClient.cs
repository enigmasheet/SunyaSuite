using SunyaSuite.Application.Interfaces;

namespace SunyaSuite.Web.Client.Services;

public class ExportServiceClient : IExportService
{
    private readonly HttpClient _http;

    public ExportServiceClient(HttpClient http) => _http = http;

    public async Task<byte[]> ExportClientsAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"{ApiEndpoints.Export}/clients", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task<byte[]> ExportProjectsAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"{ApiEndpoints.Export}/projects", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task<byte[]> ExportInvoicesAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"{ApiEndpoints.Export}/invoices", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task<byte[]> ExportReportsAsync(CancellationToken ct = default)
    {
        using var response = await _http.GetAsync($"{ApiEndpoints.Export}/reports", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }
}

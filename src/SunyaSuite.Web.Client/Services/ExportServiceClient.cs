using SunyaSuite.Application.Interfaces;

namespace SunyaSuite.Web.Client.Services;

public class ExportServiceClient : IExportService
{
    private readonly HttpClient _http;

    public ExportServiceClient(HttpClient http) => _http = http;

    public async Task<byte[]> ExportClientsAsync(CancellationToken ct = default) =>
        await (await _http.GetAsync($"{ApiEndpoints.Export}/clients", ct)).Content.ReadAsByteArrayAsync(ct);

    public async Task<byte[]> ExportProjectsAsync(CancellationToken ct = default) =>
        await (await _http.GetAsync($"{ApiEndpoints.Export}/projects", ct)).Content.ReadAsByteArrayAsync(ct);

    public async Task<byte[]> ExportInvoicesAsync(CancellationToken ct = default) =>
        await (await _http.GetAsync($"{ApiEndpoints.Export}/invoices", ct)).Content.ReadAsByteArrayAsync(ct);

    public async Task<byte[]> ExportReportsAsync(CancellationToken ct = default) =>
        await (await _http.GetAsync($"{ApiEndpoints.Export}/reports", ct)).Content.ReadAsByteArrayAsync(ct);
}

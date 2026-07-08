namespace SunyaSuite.Application.Interfaces;

public interface IExportService
{
    Task<byte[]> ExportClientsAsync(CancellationToken ct = default);
    Task<byte[]> ExportProjectsAsync(CancellationToken ct = default);
    Task<byte[]> ExportInvoicesAsync(CancellationToken ct = default);
    Task<byte[]> ExportReportsAsync(CancellationToken ct = default);
}

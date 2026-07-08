using SunyaSuite.Application.DTOs.Tenant;

namespace SunyaSuite.Application.Interfaces.Tenant;

public interface IDashboardService
{
    Task<DashboardStats> GetStatsAsync(Guid? fiscalYearId = null, CancellationToken ct = default);
    Task<List<RecentInvoiceDto>> GetRecentInvoicesAsync(int count = 5, CancellationToken ct = default);
}

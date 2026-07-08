using SunyaSuite.Application.DTOs.Config;

namespace SunyaSuite.Application.Interfaces.Config;

public interface ISystemDashboardService
{
    Task<SystemDashboardStats> GetStatsAsync(CancellationToken ct = default);
}

using SunyaSuite.Application.DTOs;

namespace SunyaSuite.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(string userId, string action, string entityName, string entityId, string? details = null, CancellationToken ct = default);
    Task<PagedResult<AuditLogDto>> GetRecentAsync(int page = 1, int pageSize = 50, AuditLogFilterDto? filter = null, CancellationToken ct = default);
    Task<List<string>> GetDistinctActionsAsync(CancellationToken ct = default);
    Task<List<string>> GetDistinctEntityNamesAsync(CancellationToken ct = default);
}

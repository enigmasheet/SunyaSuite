using SunyaSuite.Domain.Entities.Tenant;
using SunyaSuite.Infrastructure.Data.Tenant;

namespace SunyaSuite.Infrastructure.Services.Tenant;

internal static class AuditLogHelper
{
    public static void Add(ApplicationDbContext context, Guid companyId, string userId, string action, string entityName, string entityId, string? details, TimeProvider timeProvider)
    {
        context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Timestamp = timeProvider.GetUtcNow().UtcDateTime,
            Details = details ?? string.Empty
        });
    }
}

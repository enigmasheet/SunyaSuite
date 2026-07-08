namespace SunyaSuite.Domain.Entities.Tenant;

public class AuditLog
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Details { get; set; } = string.Empty;

    public Company CompanyInfo { get; set; } = null!;
}

namespace SunyaSuite.Domain.Entities.Tenant;

public class NotificationPreference
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool EmailEnabled { get; set; } = true;

    public Company CompanyInfo { get; set; } = null!;
}

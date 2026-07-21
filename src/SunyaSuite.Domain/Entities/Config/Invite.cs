namespace SunyaSuite.Domain.Entities.Config;

public class Invite
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string? UsedByEmail { get; set; }
    public DateTime? UsedAt { get; set; }
}

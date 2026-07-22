namespace SunyaSuite.Application.DTOs.Config;

public class InviteDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsExpiredAsOf(DateTime utcNow) => utcNow > ExpiresAt;
    public string? UsedByEmail { get; set; }
    public DateTime? UsedAt { get; set; }
}

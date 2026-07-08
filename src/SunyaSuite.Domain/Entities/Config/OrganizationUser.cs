namespace SunyaSuite.Domain.Entities.Config;

public class OrganizationUser
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public Guid? DefaultCompanyId { get; set; }
    public Guid? DefaultBranchId { get; set; }

    public Organization Organization { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}

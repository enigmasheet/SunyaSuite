namespace SunyaSuite.Domain.Entities.Tenant;

public class BusinessProfile
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PanNumber { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? LogoBase64 { get; set; }

    public Company Company { get; set; } = null!;
}

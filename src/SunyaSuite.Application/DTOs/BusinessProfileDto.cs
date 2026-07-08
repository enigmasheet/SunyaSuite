namespace SunyaSuite.Application.DTOs;

public class BusinessProfileDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PanNumber { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? LogoBase64 { get; set; }
}

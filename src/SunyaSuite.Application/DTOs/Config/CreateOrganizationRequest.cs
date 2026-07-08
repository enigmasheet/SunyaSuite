using System.ComponentModel.DataAnnotations;

namespace SunyaSuite.Application.DTOs.Config;

public class CreateOrganizationRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [RegularExpression(@"^[a-z0-9\-]+$", ErrorMessage = "Slug must contain only lowercase letters, numbers, and hyphens.")]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? DatabaseName { get; set; }

    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string OwnerEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string OwnerPassword { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string OwnerFirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string OwnerLastName { get; set; } = string.Empty;
}

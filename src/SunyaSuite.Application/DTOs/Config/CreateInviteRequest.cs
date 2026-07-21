using System.ComponentModel.DataAnnotations;

namespace SunyaSuite.Application.DTOs.Config;

public class CreateInviteRequest
{
    [Required] public Guid OrganizationId { get; set; }
    [Required] public string Role { get; set; } = string.Empty;
    public int? ExpiresInHours { get; set; }
}

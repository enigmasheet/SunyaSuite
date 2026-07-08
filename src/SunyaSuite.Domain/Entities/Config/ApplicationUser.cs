using Microsoft.AspNetCore.Identity;
using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Domain.Entities.Config;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateDisplayPreference Preference { get; set; } = DateDisplayPreference.Gregorian;

    public ICollection<OrganizationUser> OrganizationUsers { get; set; } = new List<OrganizationUser>();
}

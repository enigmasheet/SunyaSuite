using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Application.DTOs.Config;

public record OrgMembershipInfo(string OrganizationName, string Role);

public record UserDto(
    string Id,
    string Email,
    string UserName,
    string FirstName,
    string LastName,
    List<string> Roles,
    DateTime CreatedAt,
    DateDisplayPreference Preference)
{
    public List<OrgMembershipInfo> Organizations { get; init; } = [];
}

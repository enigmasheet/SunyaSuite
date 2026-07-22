namespace SunyaSuite.Application.DTOs.Config;

public record OrganizationUserDto(
    Guid Id,
    Guid OrganizationId,
    string OrganizationName,
    string OrganizationSlug,
    string UserId,
    string UserEmail,
    string UserFirstName,
    string UserLastName,
    string Role,
    DateTime JoinedAt,
    Guid? DefaultCompanyId,
    Guid? DefaultBranchId);

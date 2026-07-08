using SunyaSuite.Application.DTOs.Config;

namespace SunyaSuite.Application.Interfaces.Config;

public interface IOrganizationService
{
    Task<List<OrganizationDto>> GetMyOrganizationsAsync(string userId);
    Task<List<OrganizationDto>> GetAllAsync(CancellationToken ct = default);
    Task<(List<OrganizationDto> Items, int Total)> GetPagedAsync(int page, int pageSize, string? searchTerm = null, CancellationToken ct = default);
    Task<OrganizationDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<OrganizationDto> CreateAsync(CreateOrganizationRequest request, string adminUserId);
    Task<List<OrganizationUserDto>> GetUserOrganizationsAsync(string userId, CancellationToken ct = default);
    Task AssignToOrganizationAsync(string userId, Guid organizationId, string role, CancellationToken ct = default);
    Task UpdateOrganizationRoleAsync(string userId, Guid organizationId, string role, CancellationToken ct = default);
    Task RemoveFromOrganizationAsync(string userId, Guid organizationId, CancellationToken ct = default);
    Task<List<OrganizationUserDto>> GetOrgUsersAsync(Guid organizationId, CancellationToken ct = default);
    Task<UserDto> CreateUserForOrganizationAsync(Guid organizationId, CreateOrgUserRequest request, string adminUserId);
    Task<OrganizationDto> UpdateAsync(Guid id, UpdateOrganizationRequest request);
    Task DeleteAsync(Guid id);
    Task ToggleActiveAsync(Guid id);
    Task UpdateOrgUserRoleAsync(Guid orgId, string userId, string role);
    Task UpdateOrgUserDefaultsAsync(Guid orgId, string userId, Guid? companyId, Guid? branchId);
}

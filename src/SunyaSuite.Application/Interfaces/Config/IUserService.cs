using SunyaSuite.Application.DTOs.Config;

namespace SunyaSuite.Application.Interfaces.Config;

public interface IUserService
{
    Task<(List<UserDto> Items, int Total)> GetPagedAsync(int page, int pageSize, string? searchTerm = null, CancellationToken ct = default);
    Task<UserDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<UserDto> CreateAsync(string email, string password, string firstName, string lastName, List<string> roles, CancellationToken ct = default);
    Task<UserDto> UpdateAsync(string id, string email, string firstName, string lastName, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
    Task AssignRolesAsync(string id, List<string> roles, CancellationToken ct = default);
    Task<List<string>> GetAllRolesAsync(CancellationToken ct = default);
    Task<List<string>> GetOrgRolesAsync(CancellationToken ct = default);
    Task CreateRoleAsync(string roleName, CancellationToken ct = default);
    Task DeleteRoleAsync(string roleName, CancellationToken ct = default);
}

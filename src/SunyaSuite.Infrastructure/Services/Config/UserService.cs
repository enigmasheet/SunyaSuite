using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SunyaSuite.Application.DTOs.Config;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Domain.Entities.Config;
using SunyaSuite.Domain.Enums;
using SunyaSuite.Infrastructure.Data.Config;

namespace SunyaSuite.Infrastructure.Services.Config;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IDbContextFactory<ConfigDbContext> _configContextFactory;
    private readonly TimeProvider _timeProvider;

    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IDbContextFactory<ConfigDbContext> configContextFactory,
        TimeProvider timeProvider)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configContextFactory = configContextFactory;
        _timeProvider = timeProvider;
    }

    private async Task<Dictionary<string, List<string>>> GetRolesBatchAsync(List<string> userIds, CancellationToken ct)
    {
        await using var context = await _configContextFactory.CreateDbContextAsync(ct);
        var userRoles = await context.Set<IdentityUserRole<string>>()
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(context.Set<IdentityRole>(),
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => new { ur.UserId, RoleName = r.Name ?? "" })
            .ToListAsync(ct);

        return userRoles
            .GroupBy(ur => ur.UserId)
            .ToDictionary(g => g.Key, g => g.Select(ur => ur.RoleName).ToList());
    }

    private async Task<Dictionary<string, List<OrgMembershipInfo>>> GetOrgMembershipsBatchAsync(List<string> userIds, CancellationToken ct)
    {
        await using var context = await _configContextFactory.CreateDbContextAsync(ct);
        var memberships = await context.OrganizationUsers
            .Where(ou => userIds.Contains(ou.UserId))
            .Include(ou => ou.Organization)
            .ToListAsync(ct);

        return memberships
            .GroupBy(ou => ou.UserId)
            .ToDictionary(g => g.Key, g => g.Select(ou =>
                new OrgMembershipInfo(ou.Organization.Name, ou.Role)).ToList());
    }

    public async Task<(List<UserDto> Items, int Total)> GetPagedAsync(int page, int pageSize, string? searchTerm = null, CancellationToken ct = default)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(u =>
                u.Email!.ToLower().Contains(term) ||
                u.UserName!.ToLower().Contains(term) ||
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var userIds = users.Select(u => u.Id).ToList();
        var rolesByUser = userIds.Count > 0
            ? await GetRolesBatchAsync(userIds, ct)
            : [];

        var orgsByUser = userIds.Count > 0
            ? await GetOrgMembershipsBatchAsync(userIds, ct)
            : [];

        var items = users.Select(u => new UserDto(
            u.Id, u.Email ?? "", u.UserName ?? "", u.FirstName, u.LastName,
            rolesByUser.GetValueOrDefault(u.Id, []), u.CreatedAt, u.Preference)
        { Organizations = orgsByUser.GetValueOrDefault(u.Id, []) }).ToList();

        return (items, total);
    }

    public async Task<UserDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return null;

        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        return new UserDto(user.Id, user.Email ?? "", user.UserName ?? "", user.FirstName, user.LastName, roles, user.CreatedAt, user.Preference);
    }

    public async Task<UserDto> CreateAsync(string email, string password, string firstName, string lastName, List<string> roles, CancellationToken ct = default)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        if (roles.Count > 0)
        {
            var roleResult = await _userManager.AddToRolesAsync(user, roles);
            if (!roleResult.Succeeded)
                throw new InvalidOperationException(string.Join("; ", roleResult.Errors.Select(e => e.Description)));
        }

        return new UserDto(user.Id, user.Email, user.UserName, user.FirstName, user.LastName, roles, user.CreatedAt, user.Preference);
    }

    public async Task<UserDto> UpdateAsync(string id, string email, string firstName, string lastName, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) throw new KeyNotFoundException($"User {id} not found");

        user.Email = email;
        user.UserName = email;
        user.FirstName = firstName;
        user.LastName = lastName;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        return new UserDto(user.Id, user.Email, user.UserName, user.FirstName, user.LastName, roles, user.CreatedAt, user.Preference);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) throw new KeyNotFoundException($"User {id} not found");

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task AssignRolesAsync(string id, List<string> roles, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) throw new KeyNotFoundException($"User {id} not found");

        var currentRoles = (await _userManager.GetRolesAsync(user)).ToList();

        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
            throw new InvalidOperationException($"Failed to remove roles: {string.Join("; ", removeResult.Errors.Select(e => e.Description))}");

        if (roles.Count == 0) return;

        var addResult = await _userManager.AddToRolesAsync(user, roles);
        if (!addResult.Succeeded)
        {
            await _userManager.AddToRolesAsync(user, currentRoles);
            throw new InvalidOperationException($"Failed to assign roles: {string.Join("; ", addResult.Errors.Select(e => e.Description))}");
        }
    }

    public async Task<List<string>> GetAllRolesAsync(CancellationToken ct = default)
    {
        return await _roleManager.Roles.Select(r => r.Name!).ToListAsync(ct);
    }

    public Task<List<string>> GetOrgRolesAsync(CancellationToken ct = default)
    {
        var roles = new List<string>
        {
            OrgRoles.Owner,
            OrgRoles.OrgAdmin,
            OrgRoles.Member,
            OrgRoles.Viewer
        };
        return Task.FromResult(roles);
    }

    public async Task CreateRoleAsync(string roleName, CancellationToken ct = default)
    {
        if (await _roleManager.RoleExistsAsync(roleName))
            throw new InvalidOperationException($"Role '{roleName}' already exists.");

        var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    public async Task DeleteRoleAsync(string roleName, CancellationToken ct = default)
    {
        var role = await _roleManager.FindByNameAsync(roleName);
        if (role is null)
            throw new KeyNotFoundException($"Role '{roleName}' not found.");

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}

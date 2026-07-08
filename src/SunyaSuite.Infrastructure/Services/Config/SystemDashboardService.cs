using Microsoft.EntityFrameworkCore;
using SunyaSuite.Application.DTOs.Config;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Infrastructure.Data.Config;

namespace SunyaSuite.Infrastructure.Services.Config;

public class SystemDashboardService : ISystemDashboardService
{
    private readonly IDbContextFactory<ConfigDbContext> _configFactory;

    public SystemDashboardService(IDbContextFactory<ConfigDbContext> configFactory)
    {
        _configFactory = configFactory;
    }

    public async Task<SystemDashboardStats> GetStatsAsync(CancellationToken ct = default)
    {
        await using var configDb = await _configFactory.CreateDbContextAsync(ct);

        var totalOrganizations = await configDb.Organizations.CountAsync(ct);
        var activeOrganizations = await configDb.Organizations.CountAsync(o => o.IsActive && o.DeletedAt == null, ct);
        var deletedOrganizations = await configDb.Organizations.CountAsync(o => o.DeletedAt != null, ct);
        var separateDbOrgs = await configDb.Organizations.CountAsync(o => o.ConnectionString != null, ct);

        var totalUsers = await configDb.Users.CountAsync(ct);
        var totalOrgMemberships = await configDb.OrganizationUsers.CountAsync(ct);

        var recentOrgs = await configDb.Organizations
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .Select(o => new RecentOrgDto(
                o.Id,
                o.Name,
                o.Slug,
                o.IsActive && o.DeletedAt == null,
                o.ConnectionString != null,
                o.CreatedAt))
            .ToListAsync(ct);

        return new SystemDashboardStats
        {
            TotalOrganizations = totalOrganizations,
            ActiveOrganizations = activeOrganizations,
            DeletedOrganizations = deletedOrganizations,
            SeparateDbOrgs = separateDbOrgs,
            TotalUsers = totalUsers,
            TotalOrgMemberships = totalOrgMemberships,
            RecentOrganizations = recentOrgs
        };
    }
}

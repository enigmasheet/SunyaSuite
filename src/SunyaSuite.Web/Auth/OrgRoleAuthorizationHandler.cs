using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Domain.Constants;
using SunyaSuite.Domain.Enums;
using SunyaSuite.Infrastructure.Data.Config;
using System.Security.Claims;

namespace SunyaSuite.Web.Api.Auth;

internal static class OrgRoleMatcher
{
    public static bool MatchesRequirement(string role, IAuthorizationRequirement requirement) => requirement switch
    {
        OrgViewerRequirement => role is OrgRoles.Owner or OrgRoles.OrgAdmin or OrgRoles.Member or OrgRoles.Viewer,
        OrgMemberRequirement => role is OrgRoles.Owner or OrgRoles.OrgAdmin or OrgRoles.Member,
        OrgAdminRequirement => role is OrgRoles.Owner or OrgRoles.OrgAdmin,
        _ => false
    };
}

public class OrgAdminRequirement : IAuthorizationRequirement { }
public class OrgMemberRequirement : IAuthorizationRequirement { }
public class OrgViewerRequirement : IAuthorizationRequirement { }

public class OrgRoleAuthorizationHandler : AuthorizationHandler<IAuthorizationRequirement>
{
    private readonly ITenantContext _tenantContext;
    private readonly IDbContextFactory<ConfigDbContext> _configFactory;

    public OrgRoleAuthorizationHandler(
        ITenantContext tenantContext,
        IDbContextFactory<ConfigDbContext> configFactory)
    {
        _tenantContext = tenantContext;
        _configFactory = configFactory;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        IAuthorizationRequirement requirement)
    {
        if (context.User.IsInRole(RoleNames.SystemAdmin))
        {
            context.Succeed(requirement);
            return;
        }

        if (!_tenantContext.HasTenant)
        {
            context.Fail();
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null)
        {
            context.Fail();
            return;
        }

        // Fast path: read org role from JWT claim (avoids DB round-trip)
        var orgRoleClaim = context.User.FindAll(ClaimNames.OrgRole)
            .Select(c => c.Value.Split(':', 2))
            .Where(parts => parts.Length == 2
                && Guid.TryParse(parts[0], out var claimOrgId)
                && claimOrgId == _tenantContext.OrganizationId)
            .Select(parts => parts[1])
            .FirstOrDefault();

        if (orgRoleClaim is not null)
        {
            if (OrgRoleMatcher.MatchesRequirement(orgRoleClaim, requirement))
                context.Succeed(requirement);
            else
                context.Fail();
            return;
        }

        // Fallback: query DB (handles old tokens without org_role claim)
        await using var configDb = await _configFactory.CreateDbContextAsync();

        var orgUser = await configDb.OrganizationUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(ou =>
                ou.OrganizationId == _tenantContext.OrganizationId &&
                ou.UserId == userId);

        if (orgUser is null)
        {
            context.Fail();
            return;
        }

        if (OrgRoleMatcher.MatchesRequirement(orgUser.Role, requirement))
            context.Succeed(requirement);
    }
}

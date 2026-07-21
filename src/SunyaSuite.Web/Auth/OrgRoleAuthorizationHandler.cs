using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Domain.Constants;
using SunyaSuite.Domain.Enums;
using SunyaSuite.Infrastructure.Data.Config;
using System.Security.Claims;

namespace SunyaSuite.Web.Api.Auth;

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

        var role = orgUser.Role;

        switch (requirement)
        {
            case OrgViewerRequirement when role is OrgRoles.Owner or OrgRoles.OrgAdmin or OrgRoles.Member or OrgRoles.Viewer:
                context.Succeed(requirement);
                break;
            case OrgMemberRequirement when role is OrgRoles.Owner or OrgRoles.OrgAdmin or OrgRoles.Member:
                context.Succeed(requirement);
                break;
            case OrgAdminRequirement when role is OrgRoles.Owner or OrgRoles.OrgAdmin:
                context.Succeed(requirement);
                break;
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SunyaSuite.Application.DTOs.Config;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Domain.Constants;
using SunyaSuite.Domain.Enums;
using SunyaSuite.Infrastructure.Data.Config;
using System.Security.Claims;

namespace SunyaSuite.Web.Api.Controllers.Config;

[ApiController]
[Route("api/auth/menu")]
[Authorize(Policy = PolicyNames.OrgViewerOrAbove)]
public class MenuController : ControllerBase
{
    private readonly ITenantContext _tenantContext;
    private readonly IDbContextFactory<ConfigDbContext> _configFactory;
    private readonly IConfiguration _configuration;

    public MenuController(ITenantContext tenantContext, IDbContextFactory<ConfigDbContext> configFactory, IConfiguration configuration)
    {
        _tenantContext = tenantContext;
        _configFactory = configFactory;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<List<MenuSectionDto>>> GetMenu()
    {
        var sections = new List<MenuSectionDto>();

        var isSystemAdmin = User.IsInRole(RoleNames.SystemAdmin);
        string? orgRole = null;

        if (_tenantContext.HasTenant)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is not null)
            {
                await using var configDb = await _configFactory.CreateDbContextAsync();
                orgRole = await configDb.OrganizationUsers
                    .AsNoTracking()
                    .Where(ou => ou.OrganizationId == _tenantContext.OrganizationId && ou.UserId == userId)
                    .Select(ou => ou.Role)
                    .FirstOrDefaultAsync();
            }
        }

        var isOrgViewerOrAbove = orgRole is OrgRoles.Owner or OrgRoles.OrgAdmin or OrgRoles.Member or OrgRoles.Viewer;
        var isOrgMemberOrAbove = orgRole is OrgRoles.Owner or OrgRoles.OrgAdmin or OrgRoles.Member;
        var isOrgAdminOrAbove = orgRole is OrgRoles.Owner or OrgRoles.OrgAdmin;

        if (isOrgViewerOrAbove)
        {
            sections.Add(new MenuSectionDto
            {
                SectionTitle = "Main",
                Items =
                [
                    new MenuItemDto { Label = "Dashboard", Icon = "Dashboard", Href = "/" },
                    new MenuItemDto { Label = "Clients", Icon = "People", Href = "/clients" },
                    new MenuItemDto { Label = "Projects", Icon = "WorkHistory", Href = "/projects" },
                    new MenuItemDto { Label = "Invoices", Icon = "Receipt", Href = "/invoices" },
                    new MenuItemDto { Label = "Receipts", Icon = "ReceiptLong", Href = "/receipts" },
                ]
            });
        }

        if (isSystemAdmin || isOrgAdminOrAbove)
        {
            var orgId = _tenantContext.OrganizationId;
            sections.Add(new MenuSectionDto
            {
                SectionTitle = "Organization",
                Items =
                [
                    new MenuItemDto { Label = "Users", Icon = "People", Href = $"/organizations/{orgId}/users" },
                    new MenuItemDto { Label = "Companies", Icon = "Business", Href = "/admin/companies" },
                    new MenuItemDto { Label = "Branches", Icon = "AccountTree", Href = "/admin/branches" },
                    new MenuItemDto { Label = "Fiscal Years", Icon = "CalendarMonth", Href = "/admin/fiscal-years" },
                    new MenuItemDto { Label = "Audit Log", Icon = "History", Href = "/audit" },
                ]
            });
        }

        if (isSystemAdmin)
        {
            var seqUrl = _configuration["Seq:Url"];
            var items = new List<MenuItemDto>
            {
                new() { Label = "Reports", Icon = "Assessment", Href = "/reports" },
                new() { Label = "Users", Icon = "Person", Href = "/users" },
                new() { Label = "Trash", Icon = "DeleteSweep", Href = "/trash" },
                new() { Label = "Organizations", Icon = "Business", Href = "/admin/organizations" },
                new() { Label = "Dashboard", Icon = "Dashboard", Href = "/admin/dashboard" },
                new() { Label = "Roles", Icon = "Badge", Href = "/admin/roles" },
            };

            if (!string.IsNullOrEmpty(seqUrl))
            {
                items.Add(new MenuItemDto
                {
                    Label = "Seq Logs",
                    Icon = "MonitorHeart",
                    Href = seqUrl,
                    IsExternal = true
                });
            }

            sections.Add(new MenuSectionDto
            {
                SectionTitle = "System Admin",
                Items = items
            });
        }

        sections.Add(new MenuSectionDto
        {
            SectionTitle = "Account",
            Items =
            [
                new MenuItemDto { Label = "Notifications", Icon = "Notifications", Href = "/account/notifications" },
                new MenuItemDto { Label = "Settings", Icon = "Settings", Href = "/Account/Manage" },
            ]
        });

        return Ok(sections);
    }
}

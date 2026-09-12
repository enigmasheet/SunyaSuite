using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.DTOs.Config;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Domain.Constants;
using SunyaSuite.Domain.Enums;
using SunyaSuite.Infrastructure.Data.Config;

namespace SunyaSuite.Web.Api.Controllers.Config;

[ApiController]
[Route("api/invites")]
[Authorize(Policy = PolicyNames.OrgAdminOrAbove)]
public class InvitesController : ControllerBase
{
    private readonly IInviteService _inviteService;
    private readonly ITenantContext _tenantContext;
    private readonly IDbContextFactory<ConfigDbContext> _configFactory;

    public InvitesController(
        IInviteService inviteService,
        ITenantContext tenantContext,
        IDbContextFactory<ConfigDbContext> configFactory
    )
    {
        _inviteService = inviteService;
        _tenantContext = tenantContext;
        _configFactory = configFactory;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<InviteDto>>> GetPaged(
        [FromQuery] Guid organizationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PaginationDefaults.DefaultPageSize,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default
    )
    {
        if (!IsOrgAccessible(organizationId))
            return Forbid();

        var (items, total) = await _inviteService.GetPagedAsync(
            organizationId,
            page,
            pageSize,
            searchTerm,
            ct
        );
        return Ok(new PagedResult<InviteDto>(items, total));
    }

    [HttpPost]
    public async Task<ActionResult<InviteDto>> Create(
        [FromBody] CreateInviteRequest request,
        CancellationToken ct = default
    )
    {
        if (!IsOrgAccessible(request.OrganizationId))
            return Forbid();

        var validRoles = new[]
        {
            OrgRoles.Owner,
            OrgRoles.OrgAdmin,
            OrgRoles.Member,
            OrgRoles.Viewer,
        };
        if (!validRoles.Contains(request.Role))
            return BadRequest(
                new
                {
                    error = $"Invalid role '{request.Role}'. Must be one of: {string.Join(", ", validRoles)}.",
                }
            );

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
            var invite = await _inviteService.CreateAsync(
                request.OrganizationId,
                request.Role,
                request.ExpiresInHours,
                userId,
                ct
            );
            return CreatedAtAction(
                nameof(GetPaged),
                new { organizationId = invite.OrganizationId },
                invite
            );
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        if (!await IsOrgAccessibleForInvite(id))
            return Forbid();

        await _inviteService.DeleteAsync(id, ct);
        return NoContent();
    }

    private async Task<bool> IsOrgAccessibleForInvite(Guid inviteId)
    {
        if (User.IsInRole(RoleNames.SystemAdmin))
            return true;
        if (!_tenantContext.HasTenant)
            return false;

        await using var configDb = await _configFactory.CreateDbContextAsync();
        var invite = await configDb.Invites.FindAsync(inviteId);
        return invite is not null && invite.OrganizationId == _tenantContext.OrganizationId;
    }

    private bool IsOrgAccessible(Guid orgId)
    {
        if (User.IsInRole(RoleNames.SystemAdmin))
            return true;
        return _tenantContext.HasTenant && _tenantContext.OrganizationId == orgId;
    }
}

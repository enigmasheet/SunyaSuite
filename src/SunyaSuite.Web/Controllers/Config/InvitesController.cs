using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.DTOs.Config;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Domain.Constants;
using SunyaSuite.Domain.Enums;
using System.Security.Claims;

namespace SunyaSuite.Web.Api.Controllers.Config;

[ApiController]
[Route("api/invites")]
[Authorize(Policy = PolicyNames.OrgAdminOrAbove)]
public class InvitesController : ControllerBase
{
    private readonly IInviteService _inviteService;
    private readonly ITenantContext _tenantContext;

    public InvitesController(IInviteService inviteService, ITenantContext tenantContext)
    {
        _inviteService = inviteService;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<InviteDto>>> GetPaged(
        [FromQuery] Guid organizationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        if (!IsOrgAccessible(organizationId))
            return Forbid();

        var (items, total) = await _inviteService.GetPagedAsync(organizationId, page, pageSize, searchTerm, ct);
        return Ok(new PagedResult<InviteDto>(items, total));
    }

    [HttpPost]
    public async Task<ActionResult<InviteDto>> Create([FromBody] CreateInviteRequest request, CancellationToken ct = default)
    {
        if (!IsOrgAccessible(request.OrganizationId))
            return Forbid();

        var validRoles = new[] { OrgRoles.Owner, OrgRoles.OrgAdmin, OrgRoles.Member, OrgRoles.Viewer };
        if (!validRoles.Contains(request.Role))
            return BadRequest(new { error = $"Invalid role '{request.Role}'. Must be one of: {string.Join(", ", validRoles)}." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var invite = await _inviteService.CreateAsync(request.OrganizationId, request.Role, request.ExpiresInHours, userId, ct);
        return Ok(invite);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        await _inviteService.DeleteAsync(id, ct);
        return NoContent();
    }

    private bool IsOrgAccessible(Guid orgId)
    {
        if (User.IsInRole(RoleNames.SystemAdmin))
            return true;
        return _tenantContext.HasTenant && _tenantContext.OrganizationId == orgId;
    }
}

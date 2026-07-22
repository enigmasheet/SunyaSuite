using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.DTOs.Config;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Domain.Constants;

namespace SunyaSuite.Web.Api.Controllers.Config;

[ApiController]
[Route("api/organizations")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService _orgService;
    private readonly ITenantContext _tenantContext;

    public OrganizationsController(IOrganizationService orgService, ITenantContext tenantContext)
    {
        _orgService = orgService;
        _tenantContext = tenantContext;
    }

    [HttpGet("my")]
    public async Task<ActionResult<List<OrganizationDto>>> GetMyOrganizations()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
            return Unauthorized();

        var orgs = await _orgService.GetMyOrganizationsAsync(userId);
        return Ok(orgs);
    }

    [HttpGet]
    [Authorize(Policy = PolicyNames.SystemAdminOnly)]
    public async Task<ActionResult<PagedResult<OrganizationDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var (items, total) = await _orgService.GetPagedAsync(page, pageSize, searchTerm, ct);
        return Ok(new PagedResult<OrganizationDto>(items, total));
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.SystemAdminOnly)]
    public async Task<ActionResult<OrganizationDto>> Create([FromBody] CreateOrganizationRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
            return Unauthorized();

        try
        {
            var org = await _orgService.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = org.Id }, org);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [Authorize(Policy = PolicyNames.SystemAdminOnly)]
    public async Task<ActionResult<OrganizationDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var org = await _orgService.GetByIdAsync(id, ct);
        if (org is null)
            return NotFound();
        return Ok(org);
    }

    [HttpGet("users/{userId}/orgs")]
    [Authorize(Policy = PolicyNames.SystemAdminOnly)]
    public async Task<ActionResult<List<OrganizationUserDto>>> GetUserOrganizations(string userId, CancellationToken ct = default)
    {
        var result = await _orgService.GetUserOrganizationsAsync(userId, ct);
        return Ok(result);
    }

    public record AssignOrgRequest(Guid OrganizationId, string Role);

    [HttpPost("users/{userId}/orgs")]
    [Authorize(Policy = PolicyNames.OrgAdminOrAbove)]
    public async Task<ActionResult> AssignToOrganization(string userId, [FromBody] AssignOrgRequest request, CancellationToken ct)
    {
        if (!IsTenantOrg(request.OrganizationId))
            return Forbid();

        try
        {
            await _orgService.AssignToOrganizationAsync(userId, request.OrganizationId, request.Role, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("users/{userId}/orgs/{id}")]
    [Authorize(Policy = PolicyNames.OrgAdminOrAbove)]
    public async Task<ActionResult> UpdateOrganizationRole(string userId, Guid id, [FromBody] AssignOrgRequest request, CancellationToken ct = default)
    {
        if (!IsTenantOrg(id))
            return Forbid();

        try
        {
            await _orgService.UpdateOrganizationRoleAsync(userId, id, request.Role, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("users/{userId}/orgs/{id}")]
    [Authorize(Policy = PolicyNames.OrgAdminOrAbove)]
    public async Task<ActionResult> RemoveFromOrganization(string userId, Guid id, CancellationToken ct = default)
    {
        if (!IsTenantOrg(id))
            return Forbid();

        await _orgService.RemoveFromOrganizationAsync(userId, id, ct);
        return NoContent();
    }

    [HttpGet("{id}/users")]
    [Authorize(Policy = PolicyNames.OrgAdminOrAbove)]
    public async Task<ActionResult<List<OrganizationUserDto>>> GetOrgUsers(Guid id, CancellationToken ct = default)
    {
        if (!IsTenantOrg(id))
            return Forbid();

        var users = await _orgService.GetOrgUsersAsync(id, ct);
        return Ok(users);
    }

    [HttpPost("{id}/users")]
    [Authorize(Policy = PolicyNames.OrgAdminOrAbove)]
    public async Task<ActionResult<UserDto>> CreateOrgUser(Guid id, [FromBody] CreateOrgUserRequest request)
    {
        if (!IsTenantOrg(id))
            return Forbid();

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
            return Unauthorized();

        try
        {
            var user = await _orgService.CreateUserForOrganizationAsync(id, request);
            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = PolicyNames.SystemAdminOnly)]
    public async Task<ActionResult<OrganizationDto>> Update(Guid id, [FromBody] UpdateOrganizationRequest request, CancellationToken ct = default)
    {
        try
        {
            var org = await _orgService.UpdateAsync(id, request);
            return Ok(org);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet("deleted")]
    [Authorize(Policy = PolicyNames.SystemAdminOnly)]
    public async Task<ActionResult<List<OrganizationDto>>> GetDeleted(CancellationToken ct)
    {
        var result = await _orgService.GetDeletedAsync(ct);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = PolicyNames.SystemAdminOnly)]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            await _orgService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/restore")]
    [Authorize(Policy = PolicyNames.SystemAdminOnly)]
    public async Task<ActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _orgService.RestoreAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/toggle-active")]
    [Authorize(Policy = PolicyNames.SystemAdminOnly)]
    public async Task<ActionResult> ToggleActive(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _orgService.ToggleActiveAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public record ChangeOrgRoleRequest(string Role);

    public record UpdateOrgUserDefaultsRequest(Guid? DefaultCompanyId, Guid? DefaultBranchId);

    [HttpPut("{id}/users/{userId}/defaults")]
    [Authorize(Policy = PolicyNames.OrgAdminOrAbove)]
    public async Task<ActionResult> UpdateOrgUserDefaults(Guid id, string userId, [FromBody] UpdateOrgUserDefaultsRequest request, CancellationToken ct = default)
    {
        if (!IsTenantOrg(id))
            return Forbid();

        try
        {
            await _orgService.UpdateOrgUserDefaultsAsync(id, userId, request.DefaultCompanyId, request.DefaultBranchId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("{id}/users/{userId}/role")]
    [Authorize(Policy = PolicyNames.OrgAdminOrAbove)]
    public async Task<ActionResult> UpdateOrgUserRole(Guid id, string userId, [FromBody] ChangeOrgRoleRequest request, CancellationToken ct = default)
    {
        if (!IsTenantOrg(id))
            return Forbid();

        try
        {
            await _orgService.UpdateOrgUserRoleAsync(id, userId, request.Role);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private bool IsTenantOrg(Guid id)
    {
        if (User.IsInRole(RoleNames.SystemAdmin))
            return true;
        return _tenantContext.HasTenant && _tenantContext.OrganizationId == id;
    }
}

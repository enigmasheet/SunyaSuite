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

    public OrganizationsController(IOrganizationService orgService)
    {
        _orgService = orgService;
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
            return CreatedAtAction(nameof(GetById), new { orgId = org.Id }, org);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet("{orgId:guid}")]
    [Authorize(Policy = PolicyNames.SystemAdminOnly)]
    public async Task<ActionResult<OrganizationDto>> GetById(Guid orgId, CancellationToken ct)
    {
        var org = await _orgService.GetByIdAsync(orgId, ct);
        if (org is null)
            return NotFound();
        return Ok(org);
    }

    [HttpGet("users/{userId}/orgs")]
    [Authorize(Policy = PolicyNames.SystemAdminOnly)]
    public async Task<ActionResult<List<OrganizationUserDto>>> GetUserOrganizations(string userId, CancellationToken ct)
    {
        var result = await _orgService.GetUserOrganizationsAsync(userId, ct);
        return Ok(result);
    }

    public record AssignOrgRequest(Guid OrganizationId, string Role);

    [HttpPost("users/{userId}/orgs")]
    [Authorize(Policy = PolicyNames.SystemAdminOnly)]
    public async Task<ActionResult> AssignToOrganization(string userId, [FromBody] AssignOrgRequest request, CancellationToken ct)
    {
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

    [HttpPut("users/{userId}/orgs/{orgId:guid}")]
    [Authorize(Policy = PolicyNames.SystemAdminOnly)]
    public async Task<ActionResult> UpdateOrganizationRole(string userId, Guid orgId, [FromBody] AssignOrgRequest request, CancellationToken ct)
    {
        try
        {
            await _orgService.UpdateOrganizationRoleAsync(userId, orgId, request.Role, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("users/{userId}/orgs/{orgId:guid}")]
    [Authorize(Policy = PolicyNames.SystemAdminOnly)]
    public async Task<ActionResult> RemoveFromOrganization(string userId, Guid orgId, CancellationToken ct)
    {
        await _orgService.RemoveFromOrganizationAsync(userId, orgId, ct);
        return NoContent();
    }

    [HttpGet("{orgId:guid}/users")]
    [Authorize(Policy = PolicyNames.SystemAdminOnly)]
    public async Task<ActionResult<List<OrganizationUserDto>>> GetOrgUsers(Guid orgId, CancellationToken ct)
    {
        var users = await _orgService.GetOrgUsersAsync(orgId, ct);
        return Ok(users);
    }

    [HttpPost("{orgId:guid}/users")]
    [Authorize(Policy = PolicyNames.SystemAdminOnly)]
    public async Task<ActionResult<UserDto>> CreateOrgUser(Guid orgId, [FromBody] CreateOrgUserRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
            return Unauthorized();

        try
        {
            var user = await _orgService.CreateUserForOrganizationAsync(orgId, request, userId);
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

    [HttpPut("{orgId:guid}")]
    [Authorize(Policy = PolicyNames.SystemAdminOnly)]
    public async Task<ActionResult<OrganizationDto>> Update(Guid orgId, [FromBody] UpdateOrganizationRequest request, CancellationToken ct)
    {
        try
        {
            var org = await _orgService.UpdateAsync(orgId, request);
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

    [HttpDelete("{orgId:guid}")]
    [Authorize(Policy = PolicyNames.SystemAdminOnly)]
    public async Task<ActionResult> Delete(Guid orgId)
    {
        try
        {
            await _orgService.DeleteAsync(orgId);
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

    [HttpPatch("{orgId:guid}/toggle-active")]
    [Authorize(Policy = PolicyNames.SystemAdminOnly)]
    public async Task<ActionResult> ToggleActive(Guid orgId)
    {
        try
        {
            await _orgService.ToggleActiveAsync(orgId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public record ChangeOrgRoleRequest(string Role);

    public record UpdateOrgUserDefaultsRequest(Guid? DefaultCompanyId, Guid? DefaultBranchId);

    [HttpPut("{orgId:guid}/users/{userId}/defaults")]
    [Authorize(Policy = PolicyNames.SystemAdminOnly)]
    public async Task<ActionResult> UpdateOrgUserDefaults(Guid orgId, string userId, [FromBody] UpdateOrgUserDefaultsRequest request, CancellationToken ct)
    {
        try
        {
            await _orgService.UpdateOrgUserDefaultsAsync(orgId, userId, request.DefaultCompanyId, request.DefaultBranchId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPut("{orgId:guid}/users/{userId}/role")]
    [Authorize(Policy = PolicyNames.SystemAdminOnly)]
    public async Task<ActionResult> UpdateOrgUserRole(Guid orgId, string userId, [FromBody] ChangeOrgRoleRequest request, CancellationToken ct)
    {
        try
        {
            await _orgService.UpdateOrgUserRoleAsync(orgId, userId, request.Role);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}

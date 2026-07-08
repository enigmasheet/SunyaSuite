using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.DTOs.Config;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Domain.Constants;

namespace SunyaSuite.Web.Api.Controllers.Config;

[ApiController]
[Route("api/users")]
[Authorize(Policy = PolicyNames.SystemAdminOnly)]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<UserDto>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var (items, total) = await _userService.GetPagedAsync(page, pageSize, searchTerm, ct);
        return Ok(new PagedResult<UserDto>(items, total));
    }

    [HttpGet("roles")]
    public async Task<ActionResult<List<string>>> GetAllRoles(CancellationToken ct = default)
    {
        var roles = await _userService.GetAllRolesAsync(ct);
        return Ok(roles);
    }

    [HttpGet("org-roles")]
    public async Task<ActionResult<List<string>>> GetOrgRoles(CancellationToken ct = default)
    {
        var roles = await _userService.GetOrgRolesAsync(ct);
        return Ok(roles);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetById(string id, CancellationToken ct = default)
    {
        var user = await _userService.GetByIdAsync(id, ct);
        if (user is null)
            return NotFound();
        return Ok(user);
    }

    public record CreateUserRequest(string Email, string Password, string FirstName, string LastName, List<string> Roles);

    [HttpPost]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request, CancellationToken ct = default)
    {
        try
        {
            var user = await _userService.CreateAsync(request.Email, request.Password, request.FirstName, request.LastName, request.Roles, ct);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    public record UpdateUserRequest(string Email, string FirstName, string LastName);

    [HttpPut("{id}")]
    public async Task<ActionResult<UserDto>> Update(string id, [FromBody] UpdateUserRequest request, CancellationToken ct = default)
    {
        try
        {
            var user = await _userService.UpdateAsync(id, request.Email, request.FirstName, request.LastName, ct);
            return Ok(user);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(string id, CancellationToken ct = default)
    {
        try
        {
            await _userService.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    public record AssignRolesRequest(List<string> Roles);

    [HttpPost("{id}/roles")]
    public async Task<ActionResult> AssignRoles(string id, [FromBody] AssignRolesRequest request, CancellationToken ct = default)
    {
        await _userService.AssignRolesAsync(id, request.Roles, ct);
        return NoContent();
    }

    [HttpPost("roles")]
    public async Task<ActionResult> CreateRole([FromBody] CreateRoleRequest request, CancellationToken ct = default)
    {
        try
        {
            await _userService.CreateRoleAsync(request.RoleName, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("roles/{roleName}")]
    public async Task<ActionResult> DeleteRole(string roleName, CancellationToken ct = default)
    {
        try
        {
            await _userService.DeleteRoleAsync(roleName, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}

public record CreateRoleRequest(string RoleName);

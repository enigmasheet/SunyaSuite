using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.DTOs.Config;
using SunyaSuite.Application.Interfaces.Config;
using System.Security.Claims;

namespace SunyaSuite.Web.Api.Controllers.Config;

[ApiController]
[Route("api/invites")]
[Authorize(Policy = Domain.Constants.PolicyNames.SystemAdminOnly)]
public class InvitesController : ControllerBase
{
    private readonly IInviteService _inviteService;

    public InvitesController(IInviteService inviteService)
    {
        _inviteService = inviteService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<InviteDto>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var (items, total) = await _inviteService.GetPagedAsync(page, pageSize, searchTerm, ct);
        return Ok(new PagedResult<InviteDto>(items, total));
    }

    [HttpPost]
    public async Task<ActionResult<InviteDto>> Create([FromBody] CreateInviteRequest request, CancellationToken ct = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var invite = await _inviteService.CreateAsync(request.Role, request.ExpiresInHours, userId, ct);
        return Ok(invite);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        await _inviteService.DeleteAsync(id, ct);
        return NoContent();
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.Interfaces;
using SunyaSuite.Domain.Constants;

namespace SunyaSuite.Web.Api.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Policy = PolicyNames.OrgViewerOrAbove)]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetRecent(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? action = null,
        [FromQuery] string? entityName = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        CancellationToken ct = default)
    {
        var filter = new AuditLogFilterDto(searchTerm, action, entityName, dateFrom, dateTo);
        var result = await _auditService.GetRecentAsync(page, pageSize, filter, ct);
        return Ok(result);
    }

    [HttpGet("actions")]
    public async Task<ActionResult<List<string>>> GetActions(CancellationToken ct = default)
    {
        var actions = await _auditService.GetDistinctActionsAsync(ct);
        return Ok(actions);
    }

    [HttpGet("entity-names")]
    public async Task<ActionResult<List<string>>> GetEntityNames(CancellationToken ct = default)
    {
        var names = await _auditService.GetDistinctEntityNamesAsync(ct);
        return Ok(names);
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.OrgMemberOrAbove)]
    public async Task<ActionResult> Log([FromBody] LogRequest request, CancellationToken ct = default)
    {
        await _auditService.LogAsync(request.UserId, request.Action, request.EntityName, request.EntityId, request.Details, ct);
        return NoContent();
    }

    public record LogRequest(string UserId, string Action, string EntityName, string EntityId, string? Details = null);
}

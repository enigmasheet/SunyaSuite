using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Constants;

namespace SunyaSuite.Web.Api.Controllers.Tenant;

[ApiController]
[Route("api/projects")]
[Authorize(Policy = PolicyNames.OrgViewerOrAbove)]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet("options")]
    public async Task<ActionResult<List<ProjectOptionDto>>> GetOptions([FromQuery] Guid? clientId = null, CancellationToken ct = default)
    {
        return Ok(await _projectService.GetProjectOptionsAsync(clientId, ct));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ProjectListItemDto>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortLabel = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var result = await _projectService.GetPagedAsync(page, pageSize, sortLabel, sortDirection, searchTerm, ct: ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProjectDetailDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var project = await _projectService.GetByIdAsync(id, ct);
        if (project is null)
            return NotFound();
        return Ok(project);
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.OrgMemberOrAbove)]
    public async Task<ActionResult<ProjectListItemDto>> Create([FromBody] CreateProjectRequest request, CancellationToken ct = default)
    {
        try
        {
            var project = await _projectService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = PolicyNames.OrgMemberOrAbove)]
    public async Task<ActionResult<ProjectListItemDto>> Update(Guid id, [FromBody] UpdateProjectRequest request, CancellationToken ct = default)
    {
        if (id != request.Id)
            return BadRequest("Route id does not match request id.");

        try
        {
            var updated = await _projectService.UpdateAsync(request, ct);
            return Ok(updated);
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
    [Authorize(Policy = PolicyNames.OrgMemberOrAbove)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _projectService.DeleteAsync(id, ct);
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

    [HttpGet("deleted")]
    public async Task<ActionResult<PagedResult<DeletedProjectDto>>> GetDeletedPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var result = await _projectService.GetDeletedPagedAsync(page, pageSize, searchTerm, ct);
        return Ok(result);
    }

    [HttpPost("{id}/restore")]
    [Authorize(Policy = PolicyNames.OrgMemberOrAbove)]
    public async Task<ActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        await _projectService.RestoreAsync(id, ct);
        return NoContent();
    }

    [HttpDelete("{id}/permanent")]
    [Authorize(Policy = PolicyNames.OrgMemberOrAbove)]
    public async Task<ActionResult> PermanentDelete(Guid id, CancellationToken ct = default)
    {
        await _projectService.PermanentDeleteAsync(id, ct);
        return NoContent();
    }
}

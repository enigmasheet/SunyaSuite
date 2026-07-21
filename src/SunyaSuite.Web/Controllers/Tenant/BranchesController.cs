using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Constants;

namespace SunyaSuite.Web.Controllers.Tenant;

[ApiController]
[Route("api/branches")]
[Authorize(Policy = PolicyNames.OrgViewerOrAbove)]
public class BranchesController : ControllerBase
{
    private readonly IBranchService _branchService;

    public BranchesController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    [HttpGet]
    public async Task<ActionResult<List<BranchDto>>> GetAll([FromQuery] Guid? companyId, CancellationToken ct = default)
    {
        List<BranchDto> result;
        if (companyId.HasValue)
            result = await _branchService.GetByCompanyIdAsync(companyId.Value, ct);
        else
            result = await _branchService.GetAllAsync(ct);
        return Ok(result);
    }

    [HttpGet("deleted")]
    public async Task<ActionResult<List<BranchDto>>> GetDeleted(CancellationToken ct = default)
    {
        var result = await _branchService.GetDeletedAsync(ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BranchDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _branchService.GetByIdAsync(id, ct);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.OrgAdminOrAbove)]
    public async Task<ActionResult<BranchDto>> Create([FromBody] CreateBranchRequest request, CancellationToken ct = default)
    {
        try
        {
            var result = await _branchService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
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

    [HttpPut("{id}")]
    [Authorize(Policy = PolicyNames.OrgAdminOrAbove)]
    public async Task<ActionResult<BranchDto>> Update(Guid id, [FromBody] UpdateBranchRequest request, CancellationToken ct = default)
    {
        if (id != request.Id)
            return BadRequest("Route id does not match request id.");

        try
        {
            var result = await _branchService.UpdateAsync(request, ct);
            return Ok(result);
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
    [Authorize(Policy = PolicyNames.OrgAdminOrAbove)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _branchService.SoftDeleteAsync(id, ct);
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

    [HttpPatch("{id}/restore")]
    [Authorize(Policy = PolicyNames.OrgAdminOrAbove)]
    public async Task<ActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _branchService.RestoreAsync(id, ct);
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

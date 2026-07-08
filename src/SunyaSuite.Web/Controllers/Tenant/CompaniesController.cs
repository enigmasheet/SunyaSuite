using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Constants;

namespace SunyaSuite.Web.Controllers.Tenant;

[ApiController]
[Route("api/companies")]
[Authorize(Policy = PolicyNames.OrgAdminOrAbove)]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public CompaniesController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CompanyDto>>> GetAll(CancellationToken ct = default)
    {
        var result = await _companyService.GetAllAsync(ct);
        return Ok(result);
    }

    [HttpGet("active")]
    public async Task<ActionResult<List<CompanyDto>>> GetActive(CancellationToken ct = default)
    {
        var result = await _companyService.GetActiveAsync(ct);
        return Ok(result);
    }

    [HttpGet("deleted")]
    public async Task<ActionResult<List<CompanyDto>>> GetDeleted(CancellationToken ct = default)
    {
        var result = await _companyService.GetDeletedAsync(ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CompanyDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _companyService.GetByIdAsync(id, ct);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CompanyDto>> Create([FromBody] CreateCompanyRequest request, CancellationToken ct = default)
    {
        try
        {
            var result = await _companyService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CompanyDto>> Update(Guid id, [FromBody] UpdateCompanyRequest request, CancellationToken ct = default)
    {
        if (id != request.Id)
            return BadRequest("Route id does not match request id.");

        try
        {
            var result = await _companyService.UpdateAsync(request, ct);
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
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _companyService.SoftDeleteAsync(id, ct);
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
    public async Task<ActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _companyService.RestoreAsync(id, ct);
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

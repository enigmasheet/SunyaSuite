using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Constants;

namespace SunyaSuite.Web.Api.Controllers.Tenant;

[ApiController]
[Route("api/fiscal-years")]
[Authorize(Policy = PolicyNames.OrgViewerOrAbove)]
public class FiscalYearsController : ControllerBase
{
    private readonly IFiscalYearService _fiscalYearService;

    public FiscalYearsController(IFiscalYearService fiscalYearService)
    {
        _fiscalYearService = fiscalYearService;
    }

    [HttpGet]
    public async Task<ActionResult<List<FiscalYearListItemDto>>> GetAll(CancellationToken ct = default)
    {
        var result = await _fiscalYearService.GetAllAsync(ct);
        return Ok(result);
    }

    [HttpGet("current")]
    public async Task<ActionResult<FiscalYearDto>> GetCurrent(CancellationToken ct = default)
    {
        var result = await _fiscalYearService.GetCurrentAsync(ct);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpGet("open")]
    public async Task<ActionResult<List<FiscalYearListItemDto>>> GetOpenYears(CancellationToken ct = default)
    {
        var result = await _fiscalYearService.GetOpenYearsAsync(ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FiscalYearDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _fiscalYearService.GetByIdAsync(id, ct);
        if (result is null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.OrgAdminOrAbove)]
    public async Task<ActionResult<FiscalYearListItemDto>> Create([FromBody] CreateFiscalYearRequest request, CancellationToken ct = default)
    {
        try
        {
            var result = await _fiscalYearService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{id}/toggle-open")]
    [Authorize(Policy = PolicyNames.OrgAdminOrAbove)]
    public async Task<ActionResult> ToggleOpen(Guid id, CancellationToken ct = default)
    {
        await _fiscalYearService.ToggleOpenAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id}/set-current")]
    [Authorize(Policy = PolicyNames.OrgAdminOrAbove)]
    public async Task<ActionResult> SetCurrent(Guid id, CancellationToken ct = default)
    {
        await _fiscalYearService.SetCurrentAsync(id, ct);
        return NoContent();
    }
}

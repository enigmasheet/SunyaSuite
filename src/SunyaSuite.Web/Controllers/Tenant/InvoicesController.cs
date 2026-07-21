using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Constants;
using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Web.Api.Controllers.Tenant;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PolicyNames.OrgViewerOrAbove)]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<InvoiceListItemDto>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortLabel = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var result = await _invoiceService.GetPagedAsync(page, pageSize, sortLabel, sortDirection, searchTerm, ct: ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<InvoiceDetailDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var invoice = await _invoiceService.GetByIdAsync(id, ct);
        if (invoice is null)
            return NotFound();
        return Ok(invoice);
    }

    [HttpGet("selection")]
    public async Task<ActionResult<List<InvoiceSelectionDto>>> GetSelection(
        [FromQuery] Guid? fiscalYearId = null,
        CancellationToken ct = default)
    {
        var result = await _invoiceService.GetInvoiceSelectionAsync(fiscalYearId, ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.OrgMemberOrAbove)]
    public async Task<ActionResult<InvoiceListItemDto>> Create([FromBody] CreateInvoiceRequest request, CancellationToken ct = default)
    {
        try
        {
            var invoice = await _invoiceService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = PolicyNames.OrgMemberOrAbove)]
    public async Task<ActionResult<InvoiceListItemDto>> Update(Guid id, [FromBody] UpdateInvoiceRequest request, CancellationToken ct = default)
    {
        if (id != request.Id)
            return BadRequest("Route id does not match request id.");

        try
        {
            var updated = await _invoiceService.UpdateAsync(request, ct);
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

    [HttpPatch("{id}/status")]
    [Authorize(Policy = PolicyNames.OrgMemberOrAbove)]
    public async Task<ActionResult> UpdateStatus(Guid id, [FromBody] InvoiceStatus status, CancellationToken ct = default)
    {
        await _invoiceService.UpdateStatusAsync(id, status, ct);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = PolicyNames.OrgAdminOrAbove)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        try
        {
            await _invoiceService.DeleteAsync(id, ct);
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
    public async Task<ActionResult<PagedResult<DeletedInvoiceDto>>> GetDeletedPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var result = await _invoiceService.GetDeletedPagedAsync(page, pageSize, searchTerm, ct);
        return Ok(result);
    }

    [HttpPost("{id}/restore")]
    [Authorize(Policy = PolicyNames.OrgAdminOrAbove)]
    public async Task<ActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        await _invoiceService.RestoreAsync(id, ct);
        return NoContent();
    }

    [HttpDelete("{id}/permanent")]
    [Authorize(Policy = PolicyNames.SystemAdminOnly)]
    public async Task<ActionResult> PermanentDelete(Guid id, CancellationToken ct = default)
    {
        await _invoiceService.PermanentDeleteAsync(id, ct);
        return NoContent();
    }
}

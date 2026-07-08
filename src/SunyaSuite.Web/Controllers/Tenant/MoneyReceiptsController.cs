using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Constants;

namespace SunyaSuite.Web.Api.Controllers.Tenant;

[ApiController]
[Route("api/money-receipts")]
[Authorize(Policy = PolicyNames.OrgMemberOrAbove)]
public class MoneyReceiptsController : ControllerBase
{
    private readonly IMoneyReceiptService _moneyReceiptService;

    public MoneyReceiptsController(IMoneyReceiptService moneyReceiptService)
    {
        _moneyReceiptService = moneyReceiptService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<MoneyReceiptListItemDto>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortLabel = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] Guid? fiscalYearId = null,
        CancellationToken ct = default)
    {
        var result = await _moneyReceiptService.GetPagedAsync(page, pageSize, searchTerm, sortLabel, sortDirection, fiscalYearId, ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MoneyReceiptDetailDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var receipt = await _moneyReceiptService.GetByIdAsync(id, ct);
        if (receipt is null)
            return NotFound();
        return Ok(receipt);
    }

    [HttpPost]
    public async Task<ActionResult<MoneyReceiptListItemDto>> Create([FromBody] CreateMoneyReceiptRequest request, CancellationToken ct = default)
    {
        try
        {
            var receipt = await _moneyReceiptService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = receipt.Id }, receipt);
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
            await _moneyReceiptService.DeleteAsync(id, ct);
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
    public async Task<ActionResult<PagedResult<MoneyReceiptListItemDto>>> GetDeletedPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var result = await _moneyReceiptService.GetDeletedPagedAsync(page, pageSize, searchTerm, ct);
        return Ok(result);
    }

    [HttpPost("{id}/restore")]
    public async Task<ActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        await _moneyReceiptService.RestoreAsync(id, ct);
        return NoContent();
    }

    [HttpDelete("{id}/permanent")]
    public async Task<ActionResult> PermanentDelete(Guid id, CancellationToken ct = default)
    {
        await _moneyReceiptService.PermanentDeleteAsync(id, ct);
        return NoContent();
    }
}

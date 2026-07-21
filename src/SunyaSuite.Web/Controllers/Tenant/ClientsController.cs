using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Constants;

namespace SunyaSuite.Web.Api.Controllers.Tenant;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = PolicyNames.OrgViewerOrAbove)]
public class ClientsController : ControllerBase
{
    private readonly IClientService _clientService;

    public ClientsController(IClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ClientListItemDto>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortLabel = null,
        [FromQuery] string? sortDirection = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var result = await _clientService.GetPagedAsync(page, pageSize, sortLabel, sortDirection, searchTerm, ct: ct);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClientDetailDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var client = await _clientService.GetByIdAsync(id, ct);
        if (client is null)
            return NotFound();
        return Ok(client);
    }

    [HttpGet("options")]
    public async Task<ActionResult<List<ClientOptionDto>>> GetOptions(CancellationToken ct = default)
    {
        var options = await _clientService.GetClientOptionsAsync(ct);
        return Ok(options);
    }

    [HttpPost]
    [Authorize(Policy = PolicyNames.OrgMemberOrAbove)]
    public async Task<ActionResult<ClientListItemDto>> Create([FromBody] CreateClientRequest request, CancellationToken ct = default)
    {
        try
        {
            var client = await _clientService.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetById), new { id = client.Id }, client);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = PolicyNames.OrgMemberOrAbove)]
    public async Task<ActionResult<ClientListItemDto>> Update(Guid id, [FromBody] UpdateClientRequest request, CancellationToken ct = default)
    {
        if (id != request.Id)
            return BadRequest("Route id does not match request id.");

        try
        {
            var updated = await _clientService.UpdateAsync(request, ct);
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
            await _clientService.DeleteAsync(id, ct);
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
    public async Task<ActionResult<PagedResult<DeletedClientDto>>> GetDeletedPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        var result = await _clientService.GetDeletedPagedAsync(page, pageSize, searchTerm, ct);
        return Ok(result);
    }

    [HttpPost("{id}/restore")]
    [Authorize(Policy = PolicyNames.OrgMemberOrAbove)]
    public async Task<ActionResult> Restore(Guid id, CancellationToken ct = default)
    {
        await _clientService.RestoreAsync(id, ct);
        return NoContent();
    }

    [HttpDelete("{id}/permanent")]
    [Authorize(Policy = PolicyNames.OrgMemberOrAbove)]
    public async Task<ActionResult> PermanentDelete(Guid id, CancellationToken ct = default)
    {
        await _clientService.PermanentDeleteAsync(id, ct);
        return NoContent();
    }
}

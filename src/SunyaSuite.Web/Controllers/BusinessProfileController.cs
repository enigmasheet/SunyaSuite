using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.Interfaces;

namespace SunyaSuite.Web.Api.Controllers;

[ApiController]
[Route("api/business-profile")]
[Authorize]
public class BusinessProfileController : ControllerBase
{
    private readonly IBusinessProfileService _businessProfileService;

    public BusinessProfileController(IBusinessProfileService businessProfileService)
    {
        _businessProfileService = businessProfileService;
    }

    [HttpGet("default")]
    public async Task<ActionResult<BusinessProfileDto>> GetDefault(CancellationToken ct = default)
    {
        var profile = await _businessProfileService.GetDefaultAsync(ct);
        if (profile is null)
            return NotFound();
        return Ok(profile);
    }

    [HttpGet("{companyId:guid}")]
    public async Task<ActionResult<BusinessProfileDto>> GetByCompanyId(Guid companyId, CancellationToken ct = default)
    {
        var profile = await _businessProfileService.GetByCompanyIdAsync(companyId, ct);
        if (profile is null)
            return NotFound();
        return Ok(profile);
    }

    [HttpPost]
    public async Task<ActionResult> SaveDefault([FromBody] BusinessProfileDto dto, CancellationToken ct = default)
    {
        await _businessProfileService.SaveDefaultAsync(dto, ct);
        return NoContent();
    }

    [HttpPut("{companyId:guid}")]
    public async Task<ActionResult> Save(Guid companyId, [FromBody] BusinessProfileDto dto, CancellationToken ct = default)
    {
        await _businessProfileService.SaveAsync(companyId, dto, ct);
        return NoContent();
    }
}

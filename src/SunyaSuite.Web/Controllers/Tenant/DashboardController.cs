using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Constants;

namespace SunyaSuite.Web.Api.Controllers.Tenant;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = PolicyNames.OrgViewerOrAbove)]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardStats>> GetStats([FromQuery] Guid? fiscalYearId = null, CancellationToken ct = default)
    {
        var result = await _dashboardService.GetStatsAsync(fiscalYearId, ct);
        return Ok(result);
    }

    [HttpGet("recent")]
    public async Task<ActionResult<List<RecentInvoiceDto>>> GetRecentInvoices([FromQuery] int count = 5, CancellationToken ct = default)
    {
        var result = await _dashboardService.GetRecentInvoicesAsync(count, ct);
        return Ok(result);
    }
}

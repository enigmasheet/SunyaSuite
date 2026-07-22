using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.DTOs.Config;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Domain.Constants;

namespace SunyaSuite.Web.Api.Controllers.Config;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Policy = PolicyNames.SystemAdminOnly)]
public class SystemDashboardController : ControllerBase
{
    private readonly ISystemDashboardService _dashboardService;

    public SystemDashboardController(ISystemDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<ActionResult<SystemDashboardStats>> GetStats(CancellationToken ct = default)
    {
        var stats = await _dashboardService.GetStatsAsync(ct);
        return Ok(stats);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.Interfaces;
using SunyaSuite.Domain.Constants;

namespace SunyaSuite.Web.Api.Controllers;

[ApiController]
[Route("api/export")]
[Authorize(Policy = PolicyNames.OrgMemberOrAbove)]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;

    public ExportController(IExportService exportService)
    {
        _exportService = exportService;
    }

    [HttpGet("clients")]
    public async Task<IActionResult> ExportClients()
    {
        var bytes = await _exportService.ExportClientsAsync();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "clients.xlsx");
    }

    [HttpGet("projects")]
    public async Task<IActionResult> ExportProjects()
    {
        var bytes = await _exportService.ExportProjectsAsync();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "projects.xlsx");
    }

    [HttpGet("invoices")]
    public async Task<IActionResult> ExportInvoices()
    {
        var bytes = await _exportService.ExportInvoicesAsync();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "invoices.xlsx");
    }

    [HttpGet("reports")]
    public async Task<IActionResult> ExportReports()
    {
        var bytes = await _exportService.ExportReportsAsync();
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "reports.xlsx");
    }
}

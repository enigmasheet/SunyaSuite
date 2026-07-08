using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Constants;
using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Web.Api.Controllers.Tenant;

[ApiController]
[Route("api/invoice-pdf")]
[Authorize(Policy = PolicyNames.OrgMemberOrAbove)]
public class InvoicePdfController : ControllerBase
{
    private readonly IInvoicePdfService _invoicePdfService;

    public InvoicePdfController(IInvoicePdfService invoicePdfService)
    {
        _invoicePdfService = invoicePdfService;
    }

    public record GenerateRequest(InvoiceDetailDto Invoice, CopyType CopyType, DateDisplayPreference Preference);

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateRequest request)
    {
        var bytes = await _invoicePdfService.GeneratePdfAsync(request.Invoice, request.CopyType, request.Preference);
        return File(bytes, "application/pdf", "invoice.pdf");
    }
}

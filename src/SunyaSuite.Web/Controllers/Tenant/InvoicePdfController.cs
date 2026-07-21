using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Constants;
using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Web.Api.Controllers.Tenant;

[ApiController]
[Route("api/invoice-pdf")]
[Authorize(Policy = PolicyNames.OrgViewerOrAbove)]
public class InvoicePdfController : ControllerBase
{
    private readonly IInvoicePdfService _invoicePdfService;
    private readonly IInvoiceService _invoiceService;

    public InvoicePdfController(IInvoicePdfService invoicePdfService, IInvoiceService invoiceService)
    {
        _invoicePdfService = invoicePdfService;
        _invoiceService = invoiceService;
    }

    public record GenerateRequest(Guid InvoiceId, CopyType CopyType, DateDisplayPreference Preference);

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateRequest request, CancellationToken ct)
    {
        var invoice = await _invoiceService.GetByIdAsync(request.InvoiceId, ct);
        if (invoice is null)
            return NotFound("Invoice not found.");

        var bytes = await _invoicePdfService.GeneratePdfAsync(invoice, request.CopyType, request.Preference, ct);
        return File(bytes, "application/pdf", "invoice.pdf");
    }
}

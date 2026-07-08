using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Constants;
using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Web.Api.Controllers.Tenant;

[ApiController]
[Route("api/receipt-pdf")]
[Authorize(Policy = PolicyNames.OrgMemberOrAbove)]
public class ReceiptPdfController : ControllerBase
{
    private readonly IReceiptPdfService _receiptPdfService;

    public ReceiptPdfController(IReceiptPdfService receiptPdfService)
    {
        _receiptPdfService = receiptPdfService;
    }

    public record GenerateRequest(MoneyReceiptDetailDto Receipt, CopyType CopyType, DateDisplayPreference Preference);

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateRequest request)
    {
        var bytes = await _receiptPdfService.GeneratePdfAsync(request.Receipt, request.CopyType, request.Preference);
        return File(bytes, "application/pdf", "receipt.pdf");
    }
}

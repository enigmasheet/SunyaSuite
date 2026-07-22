using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Constants;
using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Web.Api.Controllers.Tenant;

[ApiController]
[Route("api/receipt-pdf")]
[Authorize(Policy = PolicyNames.OrgViewerOrAbove)]
public class ReceiptPdfController : ControllerBase
{
    private readonly IReceiptPdfService _receiptPdfService;
    private readonly IMoneyReceiptService _moneyReceiptService;

    public ReceiptPdfController(IReceiptPdfService receiptPdfService, IMoneyReceiptService moneyReceiptService)
    {
        _receiptPdfService = receiptPdfService;
        _moneyReceiptService = moneyReceiptService;
    }

    public record GenerateRequest(Guid ReceiptId, CopyType CopyType, DateDisplayPreference Preference);

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateRequest request, CancellationToken ct = default)
    {
        var receipt = await _moneyReceiptService.GetByIdAsync(request.ReceiptId, ct);
        if (receipt is null)
            return NotFound("Receipt not found.");

        var bytes = await _receiptPdfService.GeneratePdfAsync(receipt, request.CopyType, request.Preference, ct);
        return File(bytes, "application/pdf", "receipt.pdf");
    }
}

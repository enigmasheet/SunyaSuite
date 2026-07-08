namespace SunyaSuite.Application.DTOs.Tenant;

public sealed record ReceiptAllocationDto(
    Guid Id,
    Guid MoneyReceiptId,
    Guid InvoiceId,
    string ReceiptNumber,
    string ReceiptFiscalYear,
    decimal AllocatedAmount,
    string InvoiceNumber);

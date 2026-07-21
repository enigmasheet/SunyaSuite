using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Application.DTOs.Tenant;

public sealed record UpdateMoneyReceiptRequest(
    Guid Id,
    Guid[] InvoiceIds,
    decimal[] AllocatedAmounts,
    PaymentMethod PaymentMethod,
    string? ReferenceNo,
    string ReceivedFromName,
    string? ReceivedFromPan,
    string? ReceivedFromAddress,
    string Notes);

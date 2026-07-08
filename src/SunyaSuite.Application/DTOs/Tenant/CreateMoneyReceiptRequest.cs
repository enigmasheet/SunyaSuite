using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Application.DTOs.Tenant;

public sealed record CreateMoneyReceiptRequest(
    Guid[] InvoiceIds,
    decimal[] AllocatedAmounts,
    PaymentMethod PaymentMethod,
    string? ReferenceNo,
    string ReceivedFromName,
    string? ReceivedFromPan,
    string? ReceivedFromAddress,
    string Notes);

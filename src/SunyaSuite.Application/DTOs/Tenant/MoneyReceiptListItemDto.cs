using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Application.DTOs.Tenant;

public sealed record MoneyReceiptListItemDto(
    Guid Id,
    string ReceiptNumber,
    string ReceivedFromName,
    decimal AmountReceived,
    string AmountInWords,
    DateOnly DateAD,
    string DateBS,
    PaymentMethod PaymentMethod,
    string? ReferenceNo,
    bool IsDeleted,
    string FiscalYear);

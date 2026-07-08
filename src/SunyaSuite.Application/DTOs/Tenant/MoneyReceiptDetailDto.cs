using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Application.DTOs.Tenant;

public sealed record MoneyReceiptDetailDto(
    Guid Id,
    string ReceiptNumber,
    string FiscalYear,
    DateOnly DateAD,
    string DateBS,
    string ReceivedFromName,
    string? ReceivedFromPan,
    string? ReceivedFromAddress,
    decimal AmountReceived,
    string AmountInWords,
    PaymentMethod PaymentMethod,
    string? ReferenceNo,
    string ReceivedBy,
    string? SellerLogoBase64,
    bool IsDeleted,
    List<ReceiptAllocationDto> Allocations);

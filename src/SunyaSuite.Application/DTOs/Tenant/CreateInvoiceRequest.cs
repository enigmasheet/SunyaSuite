using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Application.DTOs.Tenant;

public sealed record CreateInvoiceRequest(
    Guid ClientId,
    BillType BillType,
    DateOnly DueDate,
    decimal DiscountAmount,
    bool IsAbbreviated,
    string BuyerPan,
    string BuyerAddress,
    Guid? BusinessProfileId,
    Guid? ProjectId,
    string? ProjectRemark,
    List<InvoiceItemDto> Items);

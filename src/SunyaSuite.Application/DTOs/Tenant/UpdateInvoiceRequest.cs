namespace SunyaSuite.Application.DTOs.Tenant;

public sealed record UpdateInvoiceRequest(
    Guid Id,
    DateOnly DueDate,
    decimal DiscountAmount,
    bool IsAbbreviated,
    string BuyerPan,
    string BuyerAddress,
    Guid? ProjectId,
    string? ProjectRemark,
    List<InvoiceItemDto> Items);

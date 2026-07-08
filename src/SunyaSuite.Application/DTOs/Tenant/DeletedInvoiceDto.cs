using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Application.DTOs.Tenant;

public sealed record DeletedInvoiceDto(
    Guid Id,
    string InvoiceNumber,
    decimal Total,
    string FiscalYear,
    BillType BillType,
    DateTime? DeletedAt);

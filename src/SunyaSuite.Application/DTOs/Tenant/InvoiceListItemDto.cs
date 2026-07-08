using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Application.DTOs.Tenant;

public sealed record InvoiceListItemDto(
    Guid Id,
    string InvoiceNumber,
    string FiscalYear,
    BillType BillType,
    Guid ClientId,
    string ClientName,
    DateOnly IssueDate,
    DateOnly DueDate,
    decimal Total,
    string Status,
    string? ProjectName);

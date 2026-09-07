namespace SunyaSuite.Application.DTOs.Tenant;

public sealed record InvoiceSelectionDto(
    Guid Id,
    string InvoiceNumber,
    string ClientName,
    string? ClientPan,
    string? ClientAddress,
    decimal Total,
    decimal AmountPaid,
    string FiscalYear);

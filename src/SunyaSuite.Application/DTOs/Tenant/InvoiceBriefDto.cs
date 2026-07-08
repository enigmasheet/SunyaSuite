namespace SunyaSuite.Application.DTOs.Tenant;

public record InvoiceBriefDto(
    Guid Id,
    string InvoiceNumber,
    DateOnly IssueDate,
    decimal Total,
    string Status);

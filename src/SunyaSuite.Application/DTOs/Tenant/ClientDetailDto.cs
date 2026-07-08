namespace SunyaSuite.Application.DTOs.Tenant;

public record ClientDetailDto(
    Guid Id,
    string Name,
    string Email,
    string Phone,
    string Company,
    string Address,
    string? PanNumber,
    DateOnly RegisteredOn,
    string Status,
    DateTime CreatedAt,
    List<ProjectBriefDto> Projects,
    List<InvoiceBriefDto> Invoices);

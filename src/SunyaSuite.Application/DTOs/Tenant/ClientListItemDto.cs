namespace SunyaSuite.Application.DTOs.Tenant;

public record ClientListItemDto(
    Guid Id,
    string Name,
    string Email,
    string Company,
    string Phone,
    string Status,
    DateTime CreatedAt,
    string? PanNumber);

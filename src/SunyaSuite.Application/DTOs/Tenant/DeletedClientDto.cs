namespace SunyaSuite.Application.DTOs.Tenant;

public record DeletedClientDto(
    Guid Id,
    string Name,
    string Company,
    DateTime? DeletedAt);

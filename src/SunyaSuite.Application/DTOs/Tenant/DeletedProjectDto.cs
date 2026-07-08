namespace SunyaSuite.Application.DTOs.Tenant;

public sealed record DeletedProjectDto(
    Guid Id,
    string Name,
    DateTime? DeletedAt);

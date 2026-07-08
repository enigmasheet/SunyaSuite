namespace SunyaSuite.Application.DTOs.Tenant;

public sealed record ProjectDetailDto(
    Guid Id,
    string Name,
    string Description,
    Guid ClientId,
    string ClientName,
    string? ClientEmail,
    string? ClientPhone,
    string Status,
    DateOnly Deadline,
    int ProgressPercent);

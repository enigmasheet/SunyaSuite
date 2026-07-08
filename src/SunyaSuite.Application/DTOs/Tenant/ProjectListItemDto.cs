namespace SunyaSuite.Application.DTOs.Tenant;

public sealed record ProjectListItemDto(
    Guid Id,
    string Name,
    Guid ClientId,
    string ClientName,
    string Status,
    DateOnly Deadline,
    int ProgressPercent);

namespace SunyaSuite.Application.DTOs.Tenant;

public record ProjectBriefDto(
    Guid Id,
    string Name,
    string Status,
    int ProgressPercent);

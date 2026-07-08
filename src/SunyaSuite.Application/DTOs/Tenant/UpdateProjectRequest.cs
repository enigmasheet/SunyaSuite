namespace SunyaSuite.Application.DTOs.Tenant;

public sealed record UpdateProjectRequest(
    Guid Id,
    Guid ClientId,
    string Name,
    string Description,
    DateOnly Deadline,
    string Status,
    int ProgressPercent);

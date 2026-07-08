namespace SunyaSuite.Application.DTOs.Tenant;

public sealed record CreateProjectRequest(
    Guid ClientId,
    string Name,
    string Description,
    DateOnly Deadline);

namespace SunyaSuite.Application.DTOs.Config;

public sealed record SystemDashboardStats
{
    public int TotalOrganizations { get; init; }
    public int ActiveOrganizations { get; init; }
    public int DeletedOrganizations { get; init; }
    public int SeparateDbOrgs { get; init; }
    public int TotalUsers { get; init; }
    public int TotalOrgMemberships { get; init; }
    public List<RecentOrgDto> RecentOrganizations { get; init; } = [];
}

public sealed record RecentOrgDto(
    Guid Id,
    string Name,
    string Slug,
    bool IsActive,
    bool HasSeparateDb,
    DateTime CreatedAt);

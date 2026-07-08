namespace SunyaSuite.Application.DTOs.Tenant;

public sealed record BranchDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    string Name,
    string Slug,
    string Address,
    string Phone,
    bool IsActive,
    DateTime CreatedAt,
    bool IsDeleted = false,
    DateTime? DeletedAt = null);

public sealed record CreateBranchRequest(
    Guid CompanyId,
    string Name,
    string Slug,
    string Address,
    string Phone);

public sealed record UpdateBranchRequest(
    Guid Id,
    Guid CompanyId,
    string Name,
    string Slug,
    string Address,
    string Phone,
    bool IsActive);

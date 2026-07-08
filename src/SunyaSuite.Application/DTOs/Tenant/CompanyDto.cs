namespace SunyaSuite.Application.DTOs.Tenant;

public sealed record CompanyDto(
    Guid Id,
    string Name,
    string Slug,
    string Email,
    string Address,
    string Phone,
    string? PanNumber,
    bool IsActive,
    DateTime CreatedAt,
    bool IsDeleted = false,
    DateTime? DeletedAt = null);

public sealed record CreateCompanyRequest(
    string Name,
    string Slug,
    string Email,
    string Address,
    string Phone,
    string? PanNumber);

public sealed record UpdateCompanyRequest(
    Guid Id,
    string Name,
    string Slug,
    string Email,
    string Address,
    string Phone,
    string? PanNumber,
    bool IsActive);

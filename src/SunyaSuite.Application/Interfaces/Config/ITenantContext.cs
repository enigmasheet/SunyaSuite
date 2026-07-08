namespace SunyaSuite.Application.Interfaces.Config;

public interface ITenantContext
{
    Guid OrganizationId { get; }
    string Slug { get; }
    string? ConnectionString { get; }
    bool HasTenant { get; }
    Guid? CompanyId { get; }
    Guid? BranchId { get; }
    void SetTenant(Guid organizationId, string slug, string? connectionString);
    void SetCompany(Guid? companyId, Guid? branchId);
}

using SunyaSuite.Application.Interfaces.Config;

namespace SunyaSuite.Infrastructure.Services.Config;

public class TenantContext : ITenantContext
{
    public Guid OrganizationId { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string? ConnectionString { get; private set; }
    public bool HasTenant { get; private set; }
    public Guid? CompanyId { get; private set; }
    public Guid? BranchId { get; private set; }

    public void SetTenant(Guid organizationId, string slug, string? connectionString)
    {
        OrganizationId = organizationId;
        Slug = slug;
        ConnectionString = connectionString;
        HasTenant = true;
    }

    public void SetCompany(Guid? companyId, Guid? branchId)
    {
        CompanyId = companyId;
        BranchId = branchId;
    }
}

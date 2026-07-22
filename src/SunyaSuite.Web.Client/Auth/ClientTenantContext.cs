using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Web.Client.Auth;

namespace SunyaSuite.Web.Client.Auth;

public class ClientTenantContext : ITenantContext
{
    private readonly OrgManager _orgManager;

    public ClientTenantContext(OrgManager orgManager)
    {
        _orgManager = orgManager;
    }

    public Guid OrganizationId { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string? ConnectionString => null;
    public bool HasTenant => OrganizationId != Guid.Empty;
    public Guid? CompanyId => null;
    public Guid? BranchId => null;

    public void SetTenant(Guid organizationId, string slug, string? connectionString)
    {
        OrganizationId = organizationId;
        Slug = slug;
    }

    public void SetCompany(Guid? companyId, Guid? branchId) { }
}

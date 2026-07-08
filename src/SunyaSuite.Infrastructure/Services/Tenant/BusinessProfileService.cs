using Microsoft.EntityFrameworkCore;
using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.Interfaces;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Domain.Entities.Tenant;
using SunyaSuite.Infrastructure.Data.Tenant;

namespace SunyaSuite.Infrastructure.Services.Tenant;

public class BusinessProfileService : IBusinessProfileService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ITenantContext _tenantContext;

    public BusinessProfileService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ITenantContext tenantContext)
    {
        _contextFactory = contextFactory;
        _tenantContext = tenantContext;
    }

    private Task<Guid> GetRequiredCompanyIdAsync(CancellationToken ct = default)
        => TenantServiceHelper.GetRequiredCompanyIdAsync(_contextFactory, _tenantContext, ct);

    public async Task<BusinessProfileDto?> GetDefaultAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var companyId = await GetRequiredCompanyIdAsync(ct);

        var profile = await context.Set<BusinessProfile>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.CompanyId == companyId, ct);

        return profile is null ? null : MapToDto(profile);
    }

    public async Task<BusinessProfileDto?> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var profile = await context.Set<BusinessProfile>()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.CompanyId == companyId, ct);

        return profile is null ? null : MapToDto(profile);
    }

    public async Task SaveDefaultAsync(BusinessProfileDto dto, CancellationToken ct = default)
    {
        var companyId = await GetRequiredCompanyIdAsync(ct);
        await SaveAsync(companyId, dto, ct);
    }

    public async Task SaveAsync(Guid companyId, BusinessProfileDto dto, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var existing = await context.Set<BusinessProfile>()
            .FirstOrDefaultAsync(p => p.CompanyId == companyId, ct);

        if (existing is null)
        {
            existing = new BusinessProfile
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId
            };
            context.Set<BusinessProfile>().Add(existing);
        }

        existing.BusinessName = dto.BusinessName.Trim();
        existing.Address = dto.Address.Trim();
        existing.PanNumber = dto.PanNumber.Trim();
        existing.Phone = dto.Phone.Trim();
        existing.LogoBase64 = dto.LogoBase64;

        await context.SaveChangesAsync(ct);
    }

    private static BusinessProfileDto MapToDto(BusinessProfile p) => new()
    {
        Id = p.Id,
        CompanyId = p.CompanyId,
        BusinessName = p.BusinessName,
        Address = p.Address,
        PanNumber = p.PanNumber,
        Phone = p.Phone,
        LogoBase64 = p.LogoBase64
    };
}

using SunyaSuite.Application.DTOs;

namespace SunyaSuite.Application.Interfaces;

public interface IBusinessProfileService
{
    Task<BusinessProfileDto?> GetDefaultAsync(CancellationToken ct = default);
    Task<BusinessProfileDto?> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default);
    Task SaveAsync(Guid companyId, BusinessProfileDto dto, CancellationToken ct = default);
    Task SaveDefaultAsync(BusinessProfileDto dto, CancellationToken ct = default);
}

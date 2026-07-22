using SunyaSuite.Application.DTOs.Tenant;

namespace SunyaSuite.Application.Interfaces.Tenant;

public interface ICompanyService
{
    Task<CompanyDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<CompanyDto>> GetAllAsync(CancellationToken ct = default);
    Task<List<CompanyDto>> GetActiveAsync(CancellationToken ct = default);
    Task<CompanyDto> CreateAsync(CreateCompanyRequest request, CancellationToken ct = default);
    Task<CompanyDto> UpdateAsync(UpdateCompanyRequest request, CancellationToken ct = default);
    Task SoftDeleteAsync(Guid id, CancellationToken ct = default);
    Task RestoreAsync(Guid id, CancellationToken ct = default);
    Task<List<CompanyDto>> GetDeletedAsync(CancellationToken ct = default);
    Task ToggleActiveAsync(Guid id, CancellationToken ct = default);
}

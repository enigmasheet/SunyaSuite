using SunyaSuite.Application.DTOs.Tenant;

namespace SunyaSuite.Application.Interfaces.Tenant;

public interface IBranchService
{
    Task<BranchDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<BranchDto>> GetAllAsync(CancellationToken ct = default);
    Task<List<BranchDto>> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default);
    Task<BranchDto> CreateAsync(CreateBranchRequest request, CancellationToken ct = default);
    Task<BranchDto> UpdateAsync(UpdateBranchRequest request, CancellationToken ct = default);
    Task SoftDeleteAsync(Guid id, CancellationToken ct = default);
    Task RestoreAsync(Guid id, CancellationToken ct = default);
    Task<List<BranchDto>> GetDeletedAsync(CancellationToken ct = default);
    Task ToggleActiveAsync(Guid id, CancellationToken ct = default);
}

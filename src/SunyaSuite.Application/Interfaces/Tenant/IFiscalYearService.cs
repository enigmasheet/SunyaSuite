using SunyaSuite.Application.DTOs.Tenant;

namespace SunyaSuite.Application.Interfaces.Tenant;

public interface IFiscalYearService
{
    Task<FiscalYearDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<FiscalYearDto?> GetCurrentAsync(CancellationToken ct = default);
    Task<List<FiscalYearListItemDto>> GetAllAsync(CancellationToken ct = default);
    Task<List<FiscalYearListItemDto>> GetOpenYearsAsync(CancellationToken ct = default);
    Task<FiscalYearListItemDto> CreateAsync(CreateFiscalYearRequest request, CancellationToken ct = default);
    Task ToggleOpenAsync(Guid id, CancellationToken ct = default);
    Task SetCurrentAsync(Guid id, CancellationToken ct = default);
}

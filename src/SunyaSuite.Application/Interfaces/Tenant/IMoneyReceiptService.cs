using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.DTOs.Tenant;

namespace SunyaSuite.Application.Interfaces.Tenant;

public interface IMoneyReceiptService
{
    Task<MoneyReceiptDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<MoneyReceiptListItemDto>> GetPagedAsync(int page, int pageSize, string? searchTerm = null, string? sortLabel = null, string? sortDirection = null, Guid? fiscalYearId = null, CancellationToken ct = default);
    Task<MoneyReceiptListItemDto> CreateAsync(CreateMoneyReceiptRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task RestoreAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<MoneyReceiptListItemDto>> GetDeletedPagedAsync(int page, int pageSize, string? searchTerm = null, CancellationToken ct = default);
    Task PermanentDeleteAsync(Guid id, CancellationToken ct = default);
}

using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Application.Interfaces.Tenant;

public interface IInvoiceService
{
    Task<InvoiceDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<InvoiceListItemDto>> GetPagedAsync(int page, int pageSize, string? sortLabel, string? sortDirection, string? searchTerm = null, InvoiceFilterDto? filter = null, CancellationToken ct = default);
    Task<InvoiceListItemDto> CreateAsync(CreateInvoiceRequest request, CancellationToken ct = default);
    Task<InvoiceListItemDto> UpdateAsync(UpdateInvoiceRequest request, CancellationToken ct = default);
    Task<List<InvoiceSelectionDto>> GetInvoiceSelectionAsync(Guid? fiscalYearId = null, CancellationToken ct = default);
    Task UpdateStatusAsync(Guid id, InvoiceStatus status, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<DeletedInvoiceDto>> GetDeletedPagedAsync(int page, int pageSize, string? searchTerm = null, CancellationToken ct = default);
    Task RestoreAsync(Guid id, CancellationToken ct = default);
    Task PermanentDeleteAsync(Guid id, CancellationToken ct = default);
}

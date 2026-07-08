using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.DTOs.Tenant;

namespace SunyaSuite.Application.Interfaces.Tenant;

public interface IClientService
{
    Task<List<ClientOptionDto>> GetClientOptionsAsync(CancellationToken ct = default);
    Task<ClientDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<ClientListItemDto>> GetPagedAsync(int page, int pageSize, string? sortLabel, string? sortDirection, string? searchTerm = null, ClientFilterDto? filter = null, CancellationToken ct = default);
    Task<ClientListItemDto> CreateAsync(CreateClientRequest request, CancellationToken ct = default);
    Task<ClientListItemDto> UpdateAsync(UpdateClientRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<DeletedClientDto>> GetDeletedPagedAsync(int page, int pageSize, string? searchTerm = null, CancellationToken ct = default);
    Task RestoreAsync(Guid id, CancellationToken ct = default);
    Task PermanentDeleteAsync(Guid id, CancellationToken ct = default);
}

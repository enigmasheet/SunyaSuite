using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.DTOs.Tenant;

namespace SunyaSuite.Application.Interfaces.Tenant;

public interface IProjectService
{
    Task<List<ProjectOptionDto>> GetProjectOptionsAsync(Guid? clientId = null, CancellationToken ct = default);
    Task<ProjectDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<ProjectListItemDto>> GetPagedAsync(int page, int pageSize, string? sortLabel, string? sortDirection, string? searchTerm = null, ProjectFilterDto? filter = null, CancellationToken ct = default);
    Task<ProjectListItemDto> CreateAsync(CreateProjectRequest request, CancellationToken ct = default);
    Task<ProjectListItemDto> UpdateAsync(UpdateProjectRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<DeletedProjectDto>> GetDeletedPagedAsync(int page, int pageSize, string? searchTerm = null, CancellationToken ct = default);
    Task RestoreAsync(Guid id, CancellationToken ct = default);
    Task PermanentDeleteAsync(Guid id, CancellationToken ct = default);
}

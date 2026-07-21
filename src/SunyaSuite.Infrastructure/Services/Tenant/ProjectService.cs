using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Entities.Tenant;
using SunyaSuite.Domain.Enums;
using SunyaSuite.Infrastructure.Data.Tenant;

namespace SunyaSuite.Infrastructure.Services.Tenant;

public class ProjectService : IProjectService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public ProjectService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        AuthenticationStateProvider authStateProvider,
        ITenantContext tenantContext,
        TimeProvider timeProvider)
    {
        _contextFactory = contextFactory;
        _authStateProvider = authStateProvider;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider;
    }

    private Task<Guid> GetRequiredCompanyIdAsync(CancellationToken ct = default)
        => TenantServiceHelper.GetRequiredCompanyIdAsync(_contextFactory, _tenantContext, ct);

    private Task<string> GetCurrentUserIdAsync()
        => TenantServiceHelper.GetCurrentUserIdAsync(_authStateProvider);

    public async Task<List<ProjectOptionDto>> GetProjectOptionsAsync(Guid? clientId = null, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var companyId = await GetRequiredCompanyIdAsync(ct);

        var query = context.Projects
            .Include(p => p.Client)
            .AsNoTracking()
            .ForCompany(companyId).Where(p => !p.IsDeleted)
            .AsQueryable();

        if (clientId.HasValue)
            query = query.Where(p => p.ClientId == clientId.Value);

        return await query
            .OrderBy(p => p.Name)
            .Select(p => new ProjectOptionDto(p.Id, p.Name, p.ClientId, p.Client.Name))
            .ToListAsync(ct);
    }

    public async Task<ProjectDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var project = await context.Projects
            .Include(p => p.Client)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        return project is null ? null : MapToDetail(project);
    }

    public async Task<PagedResult<ProjectListItemDto>> GetPagedAsync(
        int page, int pageSize, string? sortLabel, string? sortDirection,
        string? searchTerm = null, ProjectFilterDto? filter = null, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var companyId = await GetRequiredCompanyIdAsync(ct);

        var query = context.Projects
            .Include(p => p.Client)
            .AsNoTracking()
            .ForCompany(companyId).Where(p => !p.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.Description.ToLower().Contains(term));
        }

        if (filter?.Statuses is { Count: > 0 })
            query = query.Where(p => filter.Statuses.Contains(p.Status));

        if (filter?.DeadlineFrom is not null)
            query = query.Where(p => p.Deadline >= filter.DeadlineFrom.Value);

        if (filter?.DeadlineTo is not null)
            query = query.Where(p => p.Deadline <= filter.DeadlineTo.Value);

        if (filter?.ClientId is not null)
            query = query.Where(p => p.ClientId == filter.ClientId.Value);

        query = (sortLabel?.ToLower(), sortDirection?.ToLower()) switch
        {
            ("name", "desc") => query.OrderByDescending(p => p.Name),
            ("name", _) => query.OrderBy(p => p.Name),
            ("client", "desc") => query.OrderByDescending(p => p.Client.Name),
            ("client", _) => query.OrderBy(p => p.Client.Name),
            ("status", "desc") => query.OrderByDescending(p => p.Status),
            ("status", _) => query.OrderBy(p => p.Status),
            ("deadline", "desc") => query.OrderByDescending(p => p.Deadline),
            ("deadline", _) => query.OrderBy(p => p.Deadline),
            ("progress", "desc") => query.OrderByDescending(p => p.ProgressPercent),
            ("progress", _) => query.OrderBy(p => p.ProgressPercent),
            _ => query.OrderByDescending(p => p.Deadline)
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<ProjectListItemDto>(
            items.Select(MapToListItem).ToList(), total);
    }

    public async Task<ProjectListItemDto> CreateAsync(CreateProjectRequest request, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var userId = await GetCurrentUserIdAsync();
        var companyId = await GetRequiredCompanyIdAsync(ct);

        var project = new Project
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = _tenantContext.BranchId,
            ClientId = request.ClientId,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Deadline = request.Deadline,
            Status = ProjectStatus.NotStarted,
            ProgressPercent = 0
        };

        context.Projects.Add(project);
        AuditLogHelper.Add(context, companyId, userId, "Created", "Project", project.Id.ToString(), project.Name, _timeProvider);
        await context.SaveChangesAsync(ct);

        return MapToListItem(project);
    }

    public async Task<ProjectListItemDto> UpdateAsync(UpdateProjectRequest request, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var existing = await context.Projects
            .FirstOrDefaultAsync(p => p.Id == request.Id, ct);

        if (existing is null)
            throw new KeyNotFoundException($"Project {request.Id} not found");

        if (existing.IsDeleted)
            throw new InvalidOperationException("Cannot update a deleted project.");

        var userId = await GetCurrentUserIdAsync();

        existing.ClientId = request.ClientId;
        existing.Name = request.Name.Trim();
        existing.Description = request.Description.Trim();
        existing.Deadline = request.Deadline;

        if (Enum.TryParse<ProjectStatus>(request.Status, out var status))
            existing.Status = status;

        existing.ProgressPercent = request.ProgressPercent;

        AuditLogHelper.Add(context, existing.CompanyId, userId, "Updated", "Project", existing.Id.ToString(), existing.Name, _timeProvider);
        await context.SaveChangesAsync(ct);

        return MapToListItem(existing);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var project = await context.Projects.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null)
            throw new KeyNotFoundException($"Project {id} not found");

        if (project.IsDeleted)
            throw new InvalidOperationException("Project is already deleted.");

        var userId = await GetCurrentUserIdAsync();
        var name = project.Name;
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;

        project.IsDeleted = true;
        project.DeletedAt = utcNow;

        var invoices = await context.Invoices
            .Where(i => i.ProjectId == id && !i.IsDeleted)
            .ToListAsync(ct);
        foreach (var invoice in invoices)
        {
            invoice.IsDeleted = true;
            invoice.DeletedAt = utcNow;
        }

        AuditLogHelper.Add(context, project.CompanyId, userId, "SoftDeleted", "Project", id.ToString(), name, _timeProvider);
        await context.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<DeletedProjectDto>> GetDeletedPagedAsync(
        int page, int pageSize, string? searchTerm = null, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var query = context.Projects
            .IgnoreQueryFilters()
            .Include(p => p.Client)
            .AsNoTracking()
            .Where(p => p.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term));
        }

        query = query.OrderByDescending(p => p.DeletedAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<DeletedProjectDto>(
            items.Select(MapToDeleted).ToList(), total);
    }

    public async Task RestoreAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var project = await context.Projects.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null)
            throw new KeyNotFoundException($"Project {id} not found");

        if (!project.IsDeleted)
            throw new InvalidOperationException("Project is not deleted.");

        var userId = await GetCurrentUserIdAsync();

        project.IsDeleted = false;
        project.DeletedAt = null;

        AuditLogHelper.Add(context, project.CompanyId, userId, "Restored", "Project", id.ToString(), project.Name, _timeProvider);
        await context.SaveChangesAsync(ct);
    }

    public async Task PermanentDeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var project = await context.Projects.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null)
            throw new KeyNotFoundException($"Project {id} not found");

        var userId = await GetCurrentUserIdAsync();
        var name = project.Name;

        context.Projects.Remove(project);
        AuditLogHelper.Add(context, project.CompanyId, userId, "PermanentDeleted", "Project", id.ToString(), name, _timeProvider);
        await context.SaveChangesAsync(ct);
    }

    private static ProjectListItemDto MapToListItem(Project p) => new(
        p.Id, p.Name, p.ClientId, p.Client?.Name ?? "", p.Status.ToString(), p.Deadline, p.ProgressPercent);

    private static ProjectDetailDto MapToDetail(Project p) => new(
        p.Id, p.Name, p.Description, p.ClientId, p.Client?.Name ?? "",
        p.Client?.Email, p.Client?.Phone, p.Status.ToString(), p.Deadline, p.ProgressPercent);

    private static DeletedProjectDto MapToDeleted(Project p) => new(
        p.Id, p.Name, p.DeletedAt);
}

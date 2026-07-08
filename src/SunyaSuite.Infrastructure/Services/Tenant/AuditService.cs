using Microsoft.EntityFrameworkCore;
using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.Interfaces;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Domain.Entities.Tenant;
using SunyaSuite.Infrastructure.Data.Tenant;

namespace SunyaSuite.Infrastructure.Services.Tenant;

public class AuditService : IAuditService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public AuditService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ITenantContext tenantContext,
        TimeProvider timeProvider)
    {
        _contextFactory = contextFactory;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider;
    }

    private Task<Guid> GetRequiredCompanyIdAsync(CancellationToken ct = default)
        => TenantServiceHelper.GetRequiredCompanyIdAsync(_contextFactory, _tenantContext, ct);

    public async Task LogAsync(string userId, string action, string entityName, string entityId, string? details = null, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var companyId = await GetRequiredCompanyIdAsync(ct);

        context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Timestamp = _timeProvider.GetUtcNow().UtcDateTime,
            Details = details ?? string.Empty
        });

        await context.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<AuditLogDto>> GetRecentAsync(int page = 1, int pageSize = 50, AuditLogFilterDto? filter = null, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var companyId = await GetRequiredCompanyIdAsync(ct);

        var query = context.AuditLogs
            .Where(a => a.CompanyId == companyId);

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(a =>
                    a.EntityName.ToLower().Contains(term) ||
                    a.Action.ToLower().Contains(term) ||
                    a.EntityId.ToLower().Contains(term) ||
                    a.Details.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(filter.Action))
                query = query.Where(a => a.Action == filter.Action);

            if (!string.IsNullOrWhiteSpace(filter.EntityName))
                query = query.Where(a => a.EntityName == filter.EntityName);

            if (filter.DateFrom.HasValue)
                query = query.Where(a => a.Timestamp >= filter.DateFrom.Value);

            if (filter.DateTo.HasValue)
                query = query.Where(a => a.Timestamp <= filter.DateTo.Value);
        }

        query = query.OrderByDescending(a => a.Timestamp);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto>(
            items.Select(a => new AuditLogDto(
                a.Id,
                a.UserId,
                a.Action,
                a.EntityName,
                a.EntityId,
                a.Timestamp,
                a.Details)).ToList(),
            total);
    }

    public async Task<List<string>> GetDistinctActionsAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var companyId = await GetRequiredCompanyIdAsync(ct);

        return await context.AuditLogs
            .Where(a => a.CompanyId == companyId)
            .Select(a => a.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync(ct);
    }

    public async Task<List<string>> GetDistinctEntityNamesAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var companyId = await GetRequiredCompanyIdAsync(ct);

        return await context.AuditLogs
            .Where(a => a.CompanyId == companyId)
            .Select(a => a.EntityName)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync(ct);
    }
}

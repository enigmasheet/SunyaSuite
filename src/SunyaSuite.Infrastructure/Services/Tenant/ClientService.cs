using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using SunyaSuite.Application.DTOs;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Entities.Tenant;
using SunyaSuite.Domain.Enums;
using SunyaSuite.Infrastructure.Data.Tenant;

namespace SunyaSuite.Infrastructure.Services.Tenant;

public class ClientService : IClientService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly IClientStatusCalculator _statusCalculator;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public ClientService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        AuthenticationStateProvider authStateProvider,
        IClientStatusCalculator statusCalculator,
        ITenantContext tenantContext,
        TimeProvider timeProvider)
    {
        _contextFactory = contextFactory;
        _authStateProvider = authStateProvider;
        _statusCalculator = statusCalculator;
        _tenantContext = tenantContext;
        _timeProvider = timeProvider;
    }

    private Task<Guid> GetRequiredCompanyIdAsync(CancellationToken ct = default)
        => TenantServiceHelper.GetRequiredCompanyIdAsync(_contextFactory, _tenantContext, ct);

    private Task<string> GetCurrentUserIdAsync()
        => TenantServiceHelper.GetCurrentUserIdAsync(_authStateProvider);

    public async Task<List<ClientOptionDto>> GetClientOptionsAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var companyId = await GetRequiredCompanyIdAsync(ct);

        return await context.Clients
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId && !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(c => new ClientOptionDto(c.Id, c.Name, c.PanNumber, c.Address))
            .ToListAsync(ct);
    }

    public async Task<ClientDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var client = await context.Clients
            .Include(c => c.Projects)
            .Include(c => c.Invoices)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        return client is null ? null : MapToDetail(client);
    }

    public async Task<PagedResult<ClientListItemDto>> GetPagedAsync(
        int page, int pageSize, string? sortLabel, string? sortDirection,
        string? searchTerm = null, ClientFilterDto? filter = null, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var companyId = await GetRequiredCompanyIdAsync(ct);

        var query = context.Clients
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId && !c.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                c.Email.ToLower().Contains(term) ||
                c.Company.ToLower().Contains(term));
        }

        if (filter?.Statuses is { Count: > 0 })
            query = query.Where(c => filter.Statuses.Contains(c.Status));

        if (filter?.RegisteredOnFrom is not null)
            query = query.Where(c => c.RegisteredOn >= filter.RegisteredOnFrom.Value);

        if (filter?.RegisteredOnTo is not null)
            query = query.Where(c => c.RegisteredOn <= filter.RegisteredOnTo.Value);

        query = (sortLabel?.ToLower(), sortDirection?.ToLower()) switch
        {
            ("name", "desc") => query.OrderByDescending(c => c.Name),
            ("name", _) => query.OrderBy(c => c.Name),
            ("email", "desc") => query.OrderByDescending(c => c.Email),
            ("email", _) => query.OrderBy(c => c.Email),
            ("company", "desc") => query.OrderByDescending(c => c.Company),
            ("company", _) => query.OrderBy(c => c.Company),
            ("status", "desc") => query.OrderByDescending(c => c.Status),
            ("status", _) => query.OrderBy(c => c.Status),
            ("createdat", "desc") => query.OrderByDescending(c => c.CreatedAt),
            ("createdat", _) => query.OrderBy(c => c.CreatedAt),
            _ => query.OrderByDescending(c => c.CreatedAt)
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<ClientListItemDto>(
            items.Select(MapToListItem).ToList(), total);
    }

    public async Task<ClientListItemDto> CreateAsync(CreateClientRequest request, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var userId = await GetCurrentUserIdAsync();

        var companyId = await GetRequiredCompanyIdAsync(ct);

        var client = new Client
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = _tenantContext.BranchId,
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Company = request.Company.Trim(),
            Phone = request.Phone.Trim(),
            Address = request.Address.Trim(),
            PanNumber = request.PanNumber?.Trim(),
            Status = ClientStatus.Green,
            RegisteredOn = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime),
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
        };

        context.Clients.Add(client);
        AuditLogHelper.Add(context, companyId, userId, "Created", "Client", client.Id.ToString(), client.Name, _timeProvider);

        await context.SaveChangesAsync(ct);

        return MapToListItem(client);
    }

    public async Task<ClientListItemDto> UpdateAsync(UpdateClientRequest request, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var existing = await context.Clients
            .Include(c => c.Invoices)
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct);

        if (existing is null)
            throw new KeyNotFoundException($"Client {request.Id} not found");

        if (existing.IsDeleted)
            throw new InvalidOperationException("Cannot update a deleted client.");

        var userId = await GetCurrentUserIdAsync();

        existing.Name = request.Name.Trim();
        existing.Email = request.Email.Trim();
        existing.Company = request.Company.Trim();
        existing.Phone = request.Phone.Trim();
        existing.Address = request.Address.Trim();
        existing.PanNumber = request.PanNumber?.Trim();
        existing.Status = _statusCalculator.Calculate(existing.Invoices);

        var companyId = await GetRequiredCompanyIdAsync(ct);
        AuditLogHelper.Add(context, companyId, userId, "Updated", "Client", existing.Id.ToString(), existing.Name, _timeProvider);

        await context.SaveChangesAsync(ct);

        return MapToListItem(existing);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var client = await context.Clients
            .Include(c => c.Projects)
            .Include(c => c.Invoices)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (client is null)
            throw new KeyNotFoundException($"Client {id} not found");

        if (client.IsDeleted)
            throw new InvalidOperationException("Client is already deleted.");

        var userId = await GetCurrentUserIdAsync();

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        client.IsDeleted = true;
        client.DeletedAt = now;

        foreach (var project in client.Projects)
        {
            project.IsDeleted = true;
            project.DeletedAt = now;
        }

        foreach (var invoice in client.Invoices)
        {
            invoice.IsDeleted = true;
            invoice.DeletedAt = now;
        }

        var companyId = await GetRequiredCompanyIdAsync(ct);
        AuditLogHelper.Add(context, companyId, userId, "SoftDeleted", "Client", id.ToString(), client.Name, _timeProvider);

        await context.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<DeletedClientDto>> GetDeletedPagedAsync(
        int page, int pageSize, string? searchTerm = null, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var companyId = await GetRequiredCompanyIdAsync(ct);

        var query = context.Clients
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => c.CompanyId == companyId && c.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term) ||
                c.Email.ToLower().Contains(term) ||
                c.Company.ToLower().Contains(term));
        }

        query = query.OrderByDescending(c => c.DeletedAt);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<DeletedClientDto>(
            items.Select(MapToDeleted).ToList(), total);
    }

    public async Task RestoreAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var client = await context.Clients
            .IgnoreQueryFilters()
            .Include(c => c.Projects)
            .Include(c => c.Invoices)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (client is null)
            throw new KeyNotFoundException($"Client {id} not found");

        if (!client.IsDeleted)
            throw new InvalidOperationException("Client is not deleted.");

        var userId = await GetCurrentUserIdAsync();

        client.IsDeleted = false;
        client.DeletedAt = null;

        foreach (var project in client.Projects.Where(p => p.IsDeleted))
        {
            project.IsDeleted = false;
            project.DeletedAt = null;
        }

        foreach (var invoice in client.Invoices.Where(i => i.IsDeleted))
        {
            invoice.IsDeleted = false;
            invoice.DeletedAt = null;
        }

        var companyId = await GetRequiredCompanyIdAsync(ct);
        AuditLogHelper.Add(context, companyId, userId, "Restored", "Client", id.ToString(), client.Name, _timeProvider);

        await context.SaveChangesAsync(ct);
    }

    public async Task PermanentDeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var client = await context.Clients
            .IgnoreQueryFilters()
            .Include(c => c.Projects)
            .Include(c => c.Invoices)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (client is null)
            throw new KeyNotFoundException($"Client {id} not found");

        var userId = await GetCurrentUserIdAsync();
        var name = client.Name;

        context.Invoices.RemoveRange(client.Invoices);
        context.Projects.RemoveRange(client.Projects);
        context.Clients.Remove(client);

        var companyId = await GetRequiredCompanyIdAsync(ct);
        AuditLogHelper.Add(context, companyId, userId, "PermanentDeleted", "Client", id.ToString(), name, _timeProvider);

        await context.SaveChangesAsync(ct);
    }

    private static ClientListItemDto MapToListItem(Client c) => new(
        c.Id, c.Name, c.Email, c.Company, c.Phone, c.Status.ToString(), c.CreatedAt, c.PanNumber);

    private static ClientDetailDto MapToDetail(Client c) => new(
        c.Id, c.Name, c.Email, c.Phone, c.Company, c.Address, c.PanNumber,
        c.RegisteredOn, c.Status.ToString(), c.CreatedAt,
        c.Projects.Select(p => new ProjectBriefDto(p.Id, p.Name, p.Status.ToString(), p.ProgressPercent)).ToList(),
        c.Invoices.Select(i => new InvoiceBriefDto(i.Id, i.InvoiceNumber, i.IssueDate, i.Total, i.Status.ToString())).ToList());

    private static DeletedClientDto MapToDeleted(Client c) => new(
        c.Id, c.Name, c.Company, c.DeletedAt);
}

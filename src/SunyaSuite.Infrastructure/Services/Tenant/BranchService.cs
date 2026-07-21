using Microsoft.EntityFrameworkCore;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Entities.Tenant;
using SunyaSuite.Infrastructure.Data.Tenant;

namespace SunyaSuite.Infrastructure.Services.Tenant;

public class BranchService : IBranchService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly TimeProvider _timeProvider;

    public BranchService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        TimeProvider timeProvider)
    {
        _contextFactory = contextFactory;
        _timeProvider = timeProvider;
    }

    public async Task<BranchDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var branch = await context.Branches
            .AsNoTracking()
            .Include(b => b.Company)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
        return branch is null ? null : MapToDto(branch);
    }

    public async Task<List<BranchDto>> GetAllAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.Branches
            .AsNoTracking()
            .Include(b => b.Company)
            .OrderBy(b => b.Name)
            .Select(b => MapToDto(b))
            .ToListAsync(ct);
    }

    public async Task<List<BranchDto>> GetByCompanyIdAsync(Guid companyId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.Branches
            .AsNoTracking()
            .Include(b => b.Company)
            .Where(b => b.CompanyId == companyId)
            .OrderBy(b => b.Name)
            .Select(b => MapToDto(b))
            .ToListAsync(ct);
    }

    public async Task<BranchDto> CreateAsync(CreateBranchRequest request, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var companyExists = await context.Companies.AnyAsync(c => c.Id == request.CompanyId, ct);
        if (!companyExists)
            throw new KeyNotFoundException($"Company {request.CompanyId} not found.");

        var slugExists = await context.Branches.AnyAsync(b => b.CompanyId == request.CompanyId && b.Slug == request.Slug, ct);
        if (slugExists)
            throw new InvalidOperationException($"A branch with slug '{request.Slug}' already exists in this company.");

        var branch = new Branch
        {
            Id = Guid.NewGuid(),
            CompanyId = request.CompanyId,
            Name = request.Name,
            Slug = request.Slug,
            Address = request.Address,
            Phone = request.Phone,
            IsActive = true,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        context.Branches.Add(branch);
        await context.SaveChangesAsync(ct);

        // Reload with company name
        var saved = await context.Branches
            .AsNoTracking()
            .Include(b => b.Company)
            .FirstAsync(b => b.Id == branch.Id, ct);

        return MapToDto(saved);
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var branch = await context.Branches.FindAsync([id], ct);
        if (branch is null)
            throw new KeyNotFoundException($"Branch {id} not found.");

        if (branch.IsDeleted)
            throw new InvalidOperationException("Branch is already deleted.");

        branch.IsDeleted = true;
        branch.DeletedAt = _timeProvider.GetUtcNow().UtcDateTime;
        branch.IsActive = false;

        await context.SaveChangesAsync(ct);
    }

    public async Task RestoreAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var branch = await context.Branches.IgnoreQueryFilters().Include(b => b.Company).FirstOrDefaultAsync(b => b.Id == id, ct);
        if (branch is null)
            throw new KeyNotFoundException($"Branch {id} not found.");

        if (!branch.IsDeleted)
            throw new InvalidOperationException("Branch is not deleted.");

        branch.IsDeleted = false;
        branch.DeletedAt = null;

        await context.SaveChangesAsync(ct);
    }

    public async Task<List<BranchDto>> GetDeletedAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.Branches
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(b => b.Company)
            .Where(b => b.IsDeleted)
            .OrderByDescending(b => b.DeletedAt)
            .Select(b => MapToDto(b))
            .ToListAsync(ct);
    }

    public async Task<BranchDto> UpdateAsync(UpdateBranchRequest request, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var branch = await context.Branches.FindAsync([request.Id], ct);
        if (branch is null)
            throw new KeyNotFoundException($"Branch {request.Id} not found.");

        var slugExists = await context.Branches.AnyAsync(
            b => b.CompanyId == request.CompanyId && b.Slug == request.Slug && b.Id != request.Id, ct);
        if (slugExists)
            throw new InvalidOperationException($"A branch with slug '{request.Slug}' already exists in this company.");

        branch.CompanyId = request.CompanyId;
        branch.Name = request.Name;
        branch.Slug = request.Slug;
        branch.Address = request.Address;
        branch.Phone = request.Phone;
        branch.IsActive = request.IsActive;

        await context.SaveChangesAsync(ct);

        var saved = await context.Branches
            .AsNoTracking()
            .Include(b => b.Company)
            .FirstAsync(b => b.Id == branch.Id, ct);

        return MapToDto(saved);
    }

    private static BranchDto MapToDto(Branch b) => new(
        b.Id, b.CompanyId, b.Company.Name, b.Name, b.Slug, b.Address, b.Phone, b.IsActive, b.CreatedAt, b.IsDeleted, b.DeletedAt);
}

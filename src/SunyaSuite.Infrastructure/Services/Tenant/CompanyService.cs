using Microsoft.EntityFrameworkCore;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Entities.Tenant;
using SunyaSuite.Infrastructure.Data.Tenant;

namespace SunyaSuite.Infrastructure.Services.Tenant;

public class CompanyService : ICompanyService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly TimeProvider _timeProvider;

    public CompanyService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        TimeProvider timeProvider)
    {
        _contextFactory = contextFactory;
        _timeProvider = timeProvider;
    }

    public async Task<CompanyDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var company = await context.Companies.FindAsync([id], ct);
        return company is null ? null : MapToDto(company);
    }

    public async Task<List<CompanyDto>> GetAllAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.Companies
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => MapToDto(c))
            .ToListAsync(ct);
    }

    public async Task<List<CompanyDto>> GetActiveAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.Companies
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => MapToDto(c))
            .ToListAsync(ct);
    }

    public async Task<CompanyDto> CreateAsync(CreateCompanyRequest request, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var slugExists = await context.Companies.AnyAsync(c => c.Slug == request.Slug, ct);
        if (slugExists)
            throw new InvalidOperationException($"A company with slug '{request.Slug}' already exists.");

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = request.Slug,
            Email = request.Email,
            Address = request.Address,
            Phone = request.Phone,
            PanNumber = request.PanNumber ?? string.Empty,
            IsActive = true,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        context.Companies.Add(company);
        await context.SaveChangesAsync(ct);

        return MapToDto(company);
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var company = await context.Companies.FindAsync([id], ct);
        if (company is null)
            throw new KeyNotFoundException($"Company {id} not found.");

        if (company.IsDeleted)
            throw new InvalidOperationException("Company is already deleted.");

        company.IsDeleted = true;
        company.DeletedAt = _timeProvider.GetUtcNow().UtcDateTime;
        company.IsActive = false;

        await context.SaveChangesAsync(ct);
    }

    public async Task RestoreAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var company = await context.Companies.FindAsync([id], ct);
        if (company is null)
            throw new KeyNotFoundException($"Company {id} not found.");

        if (!company.IsDeleted)
            throw new InvalidOperationException("Company is not deleted.");

        company.IsDeleted = false;
        company.DeletedAt = null;

        await context.SaveChangesAsync(ct);
    }

    public async Task<List<CompanyDto>> GetDeletedAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        return await context.Companies
            .AsNoTracking()
            .Where(c => c.IsDeleted)
            .OrderByDescending(c => c.DeletedAt)
            .Select(c => MapToDto(c))
            .ToListAsync(ct);
    }

    public async Task<CompanyDto> UpdateAsync(UpdateCompanyRequest request, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var company = await context.Companies.FindAsync([request.Id], ct);
        if (company is null)
            throw new KeyNotFoundException($"Company {request.Id} not found.");

        var slugExists = await context.Companies.AnyAsync(c => c.Slug == request.Slug && c.Id != request.Id, ct);
        if (slugExists)
            throw new InvalidOperationException($"A company with slug '{request.Slug}' already exists.");

        company.Name = request.Name;
        company.Slug = request.Slug;
        company.Email = request.Email;
        company.Address = request.Address;
        company.Phone = request.Phone;
        company.PanNumber = request.PanNumber ?? string.Empty;
        company.IsActive = request.IsActive;

        await context.SaveChangesAsync(ct);

        return MapToDto(company);
    }

    private static CompanyDto MapToDto(Company c) => new(
        c.Id, c.Name, c.Slug, c.Email, c.Address, c.Phone, c.PanNumber, c.IsActive, c.CreatedAt, c.IsDeleted, c.DeletedAt);
}

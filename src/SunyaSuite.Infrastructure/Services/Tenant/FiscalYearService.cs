using Microsoft.EntityFrameworkCore;
using NepDate;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Entities.Tenant;
using SunyaSuite.Infrastructure.Data.Tenant;

namespace SunyaSuite.Infrastructure.Services.Tenant;

public class FiscalYearService : IFiscalYearService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public FiscalYearService(
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

    public async Task<FiscalYearDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var fy = await context.FiscalYears.FindAsync([id], ct);
        return fy is null ? null : MapToDto(fy);
    }

    public async Task<FiscalYearDto?> GetCurrentAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var companyId = await GetRequiredCompanyIdAsync(ct);
        var fy = await context.FiscalYears
            .FirstOrDefaultAsync(f => f.CompanyId == companyId && f.IsCurrent, ct);
        return fy is null ? null : MapToDto(fy);
    }

    public async Task<List<FiscalYearListItemDto>> GetAllAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var companyId = await GetRequiredCompanyIdAsync(ct);
        return await context.FiscalYears
            .Where(f => f.CompanyId == companyId)
            .OrderByDescending(f => f.YearName)
            .Select(f => new FiscalYearListItemDto(f.Id, f.YearName, f.StartDateBS, f.EndDateBS, f.IsOpen, f.IsCurrent))
            .ToListAsync(ct);
    }

    public async Task<List<FiscalYearListItemDto>> GetOpenYearsAsync(CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var companyId = await GetRequiredCompanyIdAsync(ct);
        return await context.FiscalYears
            .Where(f => f.CompanyId == companyId && f.IsOpen)
            .OrderByDescending(f => f.YearName)
            .Select(f => new FiscalYearListItemDto(f.Id, f.YearName, f.StartDateBS, f.EndDateBS, f.IsOpen, f.IsCurrent))
            .ToListAsync(ct);
    }

    public async Task<FiscalYearListItemDto> CreateAsync(CreateFiscalYearRequest request, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var existing = await context.FiscalYears.AnyAsync(f => f.YearName == request.YearName, ct);
        if (existing)
            throw new InvalidOperationException($"Fiscal year {request.YearName} already exists.");

        // Parse BS dates to compute AD equivalents
        var startParts = request.StartDateBS.Split('/');
        var endParts = request.EndDateBS.Split('/');
        if (startParts.Length != 3 || endParts.Length != 3)
            throw new InvalidOperationException("Dates must be in yyyy/MM/dd format.");

        var companyId = await GetRequiredCompanyIdAsync(ct);

        var startBS = new NepaliDate(
            int.Parse(startParts[0]), int.Parse(startParts[1]), int.Parse(startParts[2]));
        var endBS = new NepaliDate(
            int.Parse(endParts[0]), int.Parse(endParts[1]), int.Parse(endParts[2]));

        var fy = new FiscalYear
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            YearName = request.YearName,
            StartDateBS = request.StartDateBS,
            EndDateBS = request.EndDateBS,
            StartDateAD = DateOnly.FromDateTime(startBS.EnglishDate),
            EndDateAD = DateOnly.FromDateTime(endBS.EnglishDate),
            IsOpen = true,
            IsCurrent = false,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        context.FiscalYears.Add(fy);
        await context.SaveChangesAsync(ct);

        return new FiscalYearListItemDto(fy.Id, fy.YearName, fy.StartDateBS, fy.EndDateBS, fy.IsOpen, fy.IsCurrent);
    }

    public async Task ToggleOpenAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var fy = await context.FiscalYears.FindAsync([id], ct);
        if (fy is null)
            throw new KeyNotFoundException($"FiscalYear {id} not found");

        fy.IsOpen = !fy.IsOpen;
        await context.SaveChangesAsync(ct);
    }

    public async Task SetCurrentAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var companyId = await GetRequiredCompanyIdAsync(ct);
        var current = await context.FiscalYears
            .FirstOrDefaultAsync(f => f.CompanyId == companyId && f.IsCurrent, ct);
        if (current is not null)
        {
            current.IsCurrent = false;
        }

        var fy = await context.FiscalYears.FindAsync([id], ct);
        if (fy is null)
            throw new KeyNotFoundException($"FiscalYear {id} not found");

        fy.IsCurrent = true;
        await context.SaveChangesAsync(ct);
    }

    private static FiscalYearDto MapToDto(FiscalYear fy) => new(
        fy.Id, fy.YearName, fy.StartDateBS, fy.EndDateBS,
        fy.StartDateAD, fy.EndDateAD, fy.IsOpen, fy.IsCurrent);
}

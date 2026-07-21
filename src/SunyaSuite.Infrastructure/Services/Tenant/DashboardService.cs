using Microsoft.EntityFrameworkCore;
using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Enums;
using SunyaSuite.Infrastructure.Data.Tenant;

namespace SunyaSuite.Infrastructure.Services.Tenant;

public class DashboardService : IDashboardService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ITenantContext _tenantContext;
    private readonly TimeProvider _timeProvider;

    public DashboardService(
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

    public async Task<DashboardStats> GetStatsAsync(Guid? fiscalYearId = null, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var companyId = await GetRequiredCompanyIdAsync(ct);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var monthStart = new DateOnly(now.Year, now.Month, 1);
        var sixMonthsAgo = monthStart.AddMonths(-5);

        var totalClients = await context.Clients.ForCompany(companyId).CountAsync(c => !c.IsDeleted, ct);
        var activeProjects = await context.Projects.ForCompany(companyId).CountAsync(p => !p.IsDeleted && p.Status != ProjectStatus.Completed, ct);

        var invoiceQuery = context.Invoices.ForCompany(companyId).Where(i => !i.IsDeleted);
        if (fiscalYearId.HasValue)
            invoiceQuery = invoiceQuery.Where(i => i.FiscalYearId == fiscalYearId.Value);

        var invoiceAgg = await invoiceQuery
            .GroupBy(i => 1)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Overdue = g.Count(i => i.Status == InvoiceStatus.Overdue),
                Outstanding = g.Count(i => i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue),
                Paid = g.Count(i => i.Status == InvoiceStatus.Paid),
                RevenueMonth = g.Where(i => i.Status == InvoiceStatus.Paid && i.IssueDate >= monthStart).Sum(i => (decimal?)i.Total) ?? 0m,
                RevenueTotal = g.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => (decimal?)i.Total) ?? 0m
            })
            .FirstOrDefaultAsync(ct) ?? new { Overdue = 0, Outstanding = 0, Paid = 0, RevenueMonth = 0m, RevenueTotal = 0m };

        var clientBreakdown = await context.Clients
            .ForCompany(companyId).Where(c => !c.IsDeleted)
            .GroupBy(c => c.Status)
            .Select(g => new StatusBreakdown(g.Key.ToString(), g.Count()))
            .ToListAsync(ct);

        var projectBreakdown = await context.Projects
            .ForCompany(companyId).Where(p => !p.IsDeleted)
            .GroupBy(p => p.Status)
            .Select(g => new StatusBreakdown(g.Key.ToString(), g.Count()))
            .ToListAsync(ct);

        var invoiceBreakdown = await invoiceQuery
            .GroupBy(i => i.Status)
            .Select(g => new StatusBreakdown(g.Key.ToString(), g.Count()))
            .ToListAsync(ct);

        var monthlyRevenueQuery = context.Invoices.ForCompany(companyId).Where(i => !i.IsDeleted && i.Status == InvoiceStatus.Paid && i.IssueDate >= sixMonthsAgo);
        if (fiscalYearId.HasValue)
            monthlyRevenueQuery = monthlyRevenueQuery.Where(i => i.FiscalYearId == fiscalYearId.Value);

        var monthlyRevenueRaw = await monthlyRevenueQuery
            .GroupBy(i => new { i.IssueDate.Year, i.IssueDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(i => i.Total) })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync(ct);

        var monthlyRevenue = monthlyRevenueRaw
            .Select(m => new MonthlyRevenueDto(
                new DateTime(m.Year, m.Month, 1).ToString("MMM yyyy"),
                m.Total))
            .ToList();

        return new DashboardStats
        {
            TotalClients = totalClients,
            ActiveProjects = activeProjects,
            OverdueInvoices = invoiceAgg.Overdue,
            OutstandingInvoices = invoiceAgg.Outstanding,
            PaidInvoices = invoiceAgg.Paid,
            RevenueThisMonth = invoiceAgg.RevenueMonth,
            RevenueTotal = invoiceAgg.RevenueTotal,
            ClientStatusBreakdown = clientBreakdown,
            ProjectStatusBreakdown = projectBreakdown,
            InvoiceStatusBreakdown = invoiceBreakdown,
            MonthlyRevenue = monthlyRevenue
        };
    }

    public async Task<List<RecentInvoiceDto>> GetRecentInvoicesAsync(int count = 5, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var companyId = await GetRequiredCompanyIdAsync(ct);

        return await context.Invoices
            .Include(i => i.Client)
            .ForCompany(companyId).Where(i => !i.IsDeleted)
            .OrderByDescending(i => i.IssueDate)
            .Take(count)
            .Select(i => new RecentInvoiceDto(
                i.Id,
                i.InvoiceNumber,
                i.Client.Name,
                i.Total,
                i.DueDate,
                i.Status.ToString()))
            .ToListAsync(ct);
    }
}

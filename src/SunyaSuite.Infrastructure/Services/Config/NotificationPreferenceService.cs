using Microsoft.EntityFrameworkCore;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Domain.Entities.Tenant;
using SunyaSuite.Infrastructure.Data.Tenant;

namespace SunyaSuite.Infrastructure.Services.Config;

public class NotificationPreferenceService : INotificationPreferenceService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly ITenantContext _tenantContext;

    public NotificationPreferenceService(
        IDbContextFactory<ApplicationDbContext> contextFactory,
        ITenantContext tenantContext)
    {
        _contextFactory = contextFactory;
        _tenantContext = tenantContext;
    }

    private Task<Guid> GetRequiredCompanyIdAsync(CancellationToken ct = default)
        => TenantServiceHelper.GetRequiredCompanyIdAsync(_contextFactory, _tenantContext, ct);

    public async Task<List<NotificationPreference>> GetForUserAsync(string userId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var companyId = await GetRequiredCompanyIdAsync(ct);
        return await context.NotificationPreferences
            .Where(n => n.CompanyId == companyId && n.UserId == userId)
            .OrderBy(n => n.Type)
            .ToListAsync(ct);
    }

    public async Task ToggleAsync(string userId, string type, bool enabled, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var companyId = await GetRequiredCompanyIdAsync(ct);

        var pref = await context.NotificationPreferences
            .FirstOrDefaultAsync(n => n.CompanyId == companyId && n.UserId == userId && n.Type == type, ct);

        if (pref is null)
        {
            context.NotificationPreferences.Add(new NotificationPreference
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                UserId = userId,
                Type = type,
                EmailEnabled = enabled
            });
        }
        else
        {
            pref.EmailEnabled = enabled;
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task SeedDefaultsAsync(string userId, CancellationToken ct = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var companyId = await GetRequiredCompanyIdAsync(ct);

        var existing = await context.NotificationPreferences
            .Where(n => n.CompanyId == companyId && n.UserId == userId)
            .Select(n => n.Type)
            .ToListAsync(ct);

        var defaultTypes = new[] { "InvoiceOverdue", "InvoicePaid", "NewUserRegistered" };
        var toAdd = defaultTypes.Except(existing).ToList();

        foreach (var type in toAdd)
        {
            context.NotificationPreferences.Add(new NotificationPreference
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                UserId = userId,
                Type = type,
                EmailEnabled = true
            });
        }

        await context.SaveChangesAsync(ct);
    }
}

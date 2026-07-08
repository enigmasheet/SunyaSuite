using Microsoft.EntityFrameworkCore;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Domain.Enums;
using SunyaSuite.Infrastructure.Data.Config;

namespace SunyaSuite.Infrastructure.Services.Config;

public class UserPreferenceService : IUserPreferenceService
{
    private readonly IDbContextFactory<ConfigDbContext> _configContextFactory;

    public UserPreferenceService(IDbContextFactory<ConfigDbContext> configContextFactory)
    {
        _configContextFactory = configContextFactory;
    }

    public async Task<DateDisplayPreference> GetDateDisplayPreferenceAsync(string userId, CancellationToken ct = default)
    {
        await using var context = await _configContextFactory.CreateDbContextAsync(ct);

        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        return user?.Preference ?? DateDisplayPreference.Gregorian;
    }

    public async Task SetDateDisplayPreferenceAsync(string userId, DateDisplayPreference preference, CancellationToken ct = default)
    {
        await using var context = await _configContextFactory.CreateDbContextAsync(ct);

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is not null)
        {
            user.Preference = preference;
            await context.SaveChangesAsync(ct);
        }
    }
}

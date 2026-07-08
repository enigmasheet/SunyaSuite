namespace SunyaSuite.Application.Interfaces.Config;

public interface INotificationPreferenceService
{
    Task<List<Domain.Entities.Tenant.NotificationPreference>> GetForUserAsync(string userId, CancellationToken ct = default);
    Task ToggleAsync(string userId, string type, bool enabled, CancellationToken ct = default);
    Task SeedDefaultsAsync(string userId, CancellationToken ct = default);
}

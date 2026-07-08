using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Application.Interfaces.Config;

public interface IUserPreferenceService
{
    Task<DateDisplayPreference> GetDateDisplayPreferenceAsync(string userId, CancellationToken ct = default);
    Task SetDateDisplayPreferenceAsync(string userId, DateDisplayPreference preference, CancellationToken ct = default);
}

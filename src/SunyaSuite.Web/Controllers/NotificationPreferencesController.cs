using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Domain.Entities.Tenant;

namespace SunyaSuite.Web.Api.Controllers;

[ApiController]
[Route("api/notification-preferences")]
[Authorize]
public class NotificationPreferencesController : ControllerBase
{
    private readonly INotificationPreferenceService _notificationPreferenceService;

    public NotificationPreferencesController(INotificationPreferenceService notificationPreferenceService)
    {
        _notificationPreferenceService = notificationPreferenceService;
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<List<NotificationPreference>>> GetForUser(string userId, CancellationToken ct = default)
    {
        var preferences = await _notificationPreferenceService.GetForUserAsync(userId, ct);
        return Ok(preferences);
    }

    public record ToggleRequest(string Type, bool Enabled);

    [HttpPost("{userId}/toggle")]
    public async Task<ActionResult> Toggle(string userId, [FromBody] ToggleRequest request, CancellationToken ct = default)
    {
        await _notificationPreferenceService.ToggleAsync(userId, request.Type, request.Enabled, ct);
        return NoContent();
    }

    [HttpPost("{userId}/seed")]
    public async Task<ActionResult> SeedDefaults(string userId, CancellationToken ct = default)
    {
        await _notificationPreferenceService.SeedDefaultsAsync(userId, ct);
        return NoContent();
    }
}

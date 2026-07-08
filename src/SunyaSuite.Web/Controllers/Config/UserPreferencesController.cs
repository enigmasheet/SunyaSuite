using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Web.Api.Controllers.Config;

[ApiController]
[Route("api/user-preferences")]
[Authorize]
public class UserPreferencesController : ControllerBase
{
    private readonly IUserPreferenceService _userPreferenceService;

    public UserPreferencesController(IUserPreferenceService userPreferenceService)
    {
        _userPreferenceService = userPreferenceService;
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult<DateDisplayPreference>> GetDateDisplayPreference(string userId, CancellationToken ct = default)
    {
        var preference = await _userPreferenceService.GetDateDisplayPreferenceAsync(userId, ct);
        return Ok(preference);
    }

    [HttpPost("{userId}")]
    public async Task<ActionResult> SetDateDisplayPreference(string userId, [FromBody] DateDisplayPreference preference, CancellationToken ct = default)
    {
        await _userPreferenceService.SetDateDisplayPreferenceAsync(userId, preference, ct);
        return NoContent();
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.Interfaces.Config;
using SunyaSuite.Domain.Constants;
using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Web.Api.Controllers.Config;

[ApiController]
[Route("api/user-preferences")]
[Authorize(Policy = PolicyNames.OrgMemberOrAbove)]
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

    public record SetDateDisplayPreferenceRequest(DateDisplayPreference Preference);

    [HttpPost("{userId}")]
    public async Task<ActionResult> SetDateDisplayPreference(string userId, [FromBody] SetDateDisplayPreferenceRequest request, CancellationToken ct = default)
    {
        await _userPreferenceService.SetDateDisplayPreferenceAsync(userId, request.Preference, ct);
        return NoContent();
    }
}

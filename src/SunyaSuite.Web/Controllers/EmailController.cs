using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.Interfaces;
using SunyaSuite.Domain.Constants;

namespace SunyaSuite.Web.Api.Controllers;

[ApiController]
[Route("api/email")]
[Authorize(Policy = PolicyNames.OrgMemberOrAbove)]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;

    public EmailController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public record SendRequest(string To, string Subject, string HtmlBody);

    [HttpPost]
    public async Task<ActionResult> Send([FromBody] SendRequest request, CancellationToken ct = default)
    {
        await _emailService.SendAsync(request.To, request.Subject, request.HtmlBody, ct);
        return NoContent();
    }
}

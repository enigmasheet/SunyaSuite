using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SunyaSuite.Application.Interfaces;
using SunyaSuite.Domain.Constants;

namespace SunyaSuite.Web.Api.Controllers;

[ApiController]
[Route("api/email")]
[Authorize(Policy = PolicyNames.OrgViewerOrAbove)]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;

    public EmailController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public record SendRequest(string To, string Subject, string HtmlBody);

    [HttpPost]
    [Authorize(Policy = PolicyNames.OrgMemberOrAbove)]
    public async Task<ActionResult> Send([FromBody] SendRequest request, CancellationToken ct = default)
    {
        await _emailService.SendAsync(request.To, request.Subject, request.HtmlBody, ct);
        return NoContent();
    }

    [HttpPost("with-attachment")]
    [Authorize(Policy = PolicyNames.OrgMemberOrAbove)]
    public async Task<ActionResult> SendWithAttachment(CancellationToken ct = default)
    {
        var file = Request.Form.Files.GetFile("attachment");
        if (file is null)
            return BadRequest("Attachment file is required.");

        var to = Request.Form["to"].FirstOrDefault();
        var subject = Request.Form["subject"].FirstOrDefault();
        var htmlBody = Request.Form["htmlBody"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(to) || string.IsNullOrWhiteSpace(subject))
            return BadRequest("To and subject are required.");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        await _emailService.SendWithAttachmentAsync(to, subject, htmlBody ?? "", file.FileName, ms.ToArray(), ct);
        return NoContent();
    }
}

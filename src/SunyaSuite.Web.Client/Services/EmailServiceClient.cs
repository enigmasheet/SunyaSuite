using SunyaSuite.Application.Interfaces;
using System.Net.Http.Json;

namespace SunyaSuite.Web.Client.Services;

/// <summary>
/// Email is a server-side operation. This client implementation triggers email via API.
/// </summary>
public class EmailServiceClient : IEmailService
{
    private readonly HttpClient _http;

    public EmailServiceClient(HttpClient http) => _http = http;

    public bool IsConfigured => true; // Will be checked server-side

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync(ApiEndpoints.Email, new { to, subject, htmlBody }, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task SendWithAttachmentAsync(string to, string subject, string htmlBody, string attachmentName, byte[] attachmentBytes, CancellationToken ct = default)
    {
        var formData = new MultipartFormDataContent
        {
            { new StringContent(to), "to" },
            { new StringContent(subject), "subject" },
            { new StringContent(htmlBody), "htmlBody" },
            { new ByteArrayContent(attachmentBytes), "attachment", attachmentName }
        };
        var response = await _http.PostAsync(ApiEndpoints.Email, formData, ct);
        response.EnsureSuccessStatusCode();
    }
}

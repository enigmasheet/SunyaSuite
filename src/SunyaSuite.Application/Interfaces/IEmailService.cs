namespace SunyaSuite.Application.Interfaces;

public interface IEmailService
{
    bool IsConfigured { get; }
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
    Task SendWithAttachmentAsync(string to, string subject, string htmlBody, string attachmentName, byte[] attachmentBytes, CancellationToken ct = default);
}

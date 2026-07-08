using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MimeKit;
using SunyaSuite.Application.Settings;
using SunyaSuite.Domain.Entities.Config;

namespace SunyaSuite.Web.Api.Services.Config;

public class MailKitEmailSender : IEmailSender<ApplicationUser>
{
    private readonly EmailSettings _settings;

    public MailKitEmailSender(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendAsync(string email, string subject, string htmlMessage)
    {
        await SendEmailAsync(email, subject, htmlMessage);
    }

    public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        var body = $"""
            <p>Welcome to SunyaSuite!</p>
            <p>Please confirm your email by clicking the link below:</p>
            <p><a href="{confirmationLink}">Confirm Email</a></p>
            <p>If you did not create this account, you can ignore this message.</p>
            """;
        await SendEmailAsync(email, "Confirm your SunyaSuite account", body);
    }

    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        var body = $"""
            <p>You requested a password reset for your SunyaSuite account.</p>
            <p>Your reset code is: <strong>{resetCode}</strong></p>
            <p>If you did not request this, you can ignore this message.</p>
            """;
        await SendEmailAsync(email, "SunyaSuite password reset code", body);
    }

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        var body = $"""
            <p>You requested a password reset for your SunyaSuite account.</p>
            <p>Click the link below to reset your password:</p>
            <p><a href="{resetLink}">Reset Password</a></p>
            <p>If you did not request this, you can ignore this message.</p>
            <p>This link expires after 24 hours.</p>
            """;
        await SendEmailAsync(email, "Reset your SunyaSuite password", body);
    }

    private async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        message.To.Add(new MailboxAddress("", to));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        await client.ConnectAsync(
            _settings.SmtpHost,
            _settings.SmtpPort,
            SecureSocketOptions.SslOnConnect);

        if (!string.IsNullOrEmpty(_settings.Username))
        {
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SunyaSuite.Application.Interfaces;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Application.Settings;
using SunyaSuite.Domain.Entities.Tenant;
using SunyaSuite.Domain.Enums;
using SunyaSuite.Infrastructure.Data.Tenant;
using SunyaSuite.Infrastructure.EmailTemplates;

namespace SunyaSuite.Infrastructure.Services.Tenant;

public class OverdueBackgroundService : BackgroundService
{
    private static readonly Dictionary<string, string> CrossPlatformTimeZones = new(StringComparer.OrdinalIgnoreCase)
    {
        ["India Standard Time"] = "Asia/Kolkata",
        ["Asia/Kolkata"] = "India Standard Time",
        ["Eastern Standard Time"] = "America/New_York",
        ["America/New_York"] = "Eastern Standard Time",
        ["Central Standard Time"] = "America/Chicago",
        ["America/Chicago"] = "Central Standard Time",
        ["Mountain Standard Time"] = "America/Denver",
        ["America/Denver"] = "Mountain Standard Time",
        ["Pacific Standard Time"] = "America/Los_Angeles",
        ["America/Los_Angeles"] = "Pacific Standard Time",
        ["GMT Standard Time"] = "Europe/London",
        ["Europe/London"] = "GMT Standard Time",
        ["Central Europe Standard Time"] = "Europe/Berlin",
        ["Europe/Berlin"] = "Central Europe Standard Time",
        ["Eastern European Standard Time"] = "Europe/Bucharest",
        ["Europe/Bucharest"] = "Eastern European Standard Time",
        ["Tokyo Standard Time"] = "Asia/Tokyo",
        ["Asia/Tokyo"] = "Tokyo Standard Time",
        ["China Standard Time"] = "Asia/Shanghai",
        ["Asia/Shanghai"] = "China Standard Time",
        ["AUS Eastern Standard Time"] = "Australia/Sydney",
        ["Australia/Sydney"] = "AUS Eastern Standard Time",
        ["Pacific SA Standard Time"] = "America/Santiago",
        ["America/Santiago"] = "Pacific SA Standard Time",
        ["Greenland Standard Time"] = "America/Nuuk",
        ["America/Nuuk"] = "Greenland Standard Time",
        ["South Africa Standard Time"] = "Africa/Johannesburg",
        ["Africa/Johannesburg"] = "South Africa Standard Time",
        ["W. Australia Standard Time"] = "Australia/Perth",
        ["Australia/Perth"] = "W. Australia Standard Time",
        ["Arabian Standard Time"] = "Asia/Dubai",
        ["Asia/Dubai"] = "Arabian Standard Time",
        ["UTC"] = "UTC",
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OverdueBackgroundService> _logger;
    private readonly OverdueSchedulerSettings _schedulerSettings;
    private volatile string _lastRunDate = string.Empty;
    private readonly TimeProvider _timeProvider;

    public OverdueBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<OverdueBackgroundService> logger,
        IOptions<OverdueSchedulerSettings> schedulerSettings,
        TimeProvider timeProvider)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _schedulerSettings = schedulerSettings.Value;
        _timeProvider = timeProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OverdueBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var enabled = _schedulerSettings.Enabled;
                var runHour = _schedulerSettings.RunHour;
                var runMinute = _schedulerSettings.RunMinute;
                var timeZoneId = _schedulerSettings.TimeZone;

                var now = _timeProvider.GetUtcNow().UtcDateTime;
                var tzInfo = GetTimeZoneInfo(timeZoneId);
                var localNow = TimeZoneInfo.ConvertTimeFromUtc(now, tzInfo);

                var todayKey = localNow.ToString("yyyy-MM-dd");

                if (enabled
                    && _lastRunDate != todayKey
                    && localNow.DayOfWeek != DayOfWeek.Saturday
                    && localNow.DayOfWeek != DayOfWeek.Sunday
                    && localNow.Hour == runHour
                    && localNow.Minute == runMinute)
                {
                    _lastRunDate = todayKey;
                    await ProcessOverdueInvoicesAsync(scope, stoppingToken);

                    await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OverdueBackgroundService");
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }
    }

    private async Task ProcessOverdueInvoicesAsync(IServiceScope scope, CancellationToken ct)
    {
        await using var context = await scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<ApplicationDbContext>>()
            .CreateDbContextAsync(ct);

        var overdueInvoices = await context.Invoices
            .Include(i => i.Client)
            .Where(i => !i.IsDeleted && i.Status == InvoiceStatus.Sent && i.DueDate < DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime))
            .Take(500)
            .ToListAsync(ct);

        if (overdueInvoices.Count == 0) return;

        var auditLogs = new List<AuditLog>(overdueInvoices.Count);
        var statusCalculator = scope.ServiceProvider.GetRequiredService<IClientStatusCalculator>();

        foreach (var invoice in overdueInvoices)
        {
            invoice.Status = InvoiceStatus.Overdue;
            invoice.Client.Status = statusCalculator.Calculate(invoice.Client.Invoices);

            auditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = "System",
                Action = "StatusChanged",
                EntityName = "Invoice",
                EntityId = invoice.Id.ToString(),
                Timestamp = _timeProvider.GetUtcNow().UtcDateTime,
                Details = $"{invoice.InvoiceNumber}: Sent → Overdue (background)"
            });
        }

        context.AuditLogs.AddRange(auditLogs);
        await context.SaveChangesAsync(ct);

        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var emailSemaphore = new SemaphoreSlim(5);
        var emailTasks = overdueInvoices.Select(async invoice =>
        {
            await emailSemaphore.WaitAsync(ct);
            try
            {
                var email = invoice.Client?.Email;
                if (string.IsNullOrEmpty(email))
                    return;

                await emailService.SendAsync(
                    email,
                    $"Payment Overdue — {invoice.InvoiceNumber}",
                    InvoiceEmailTemplate.BuildOverdueNotification(
                        invoice.Client?.Name ?? "Client",
                        invoice.InvoiceNumber,
                        invoice.Total,
                        invoice.DueDate),
                    ct);

                _logger.LogInformation("Marked invoice {Number} as Overdue and notified {Email}",
                    invoice.InvoiceNumber, email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send overdue email for invoice {Number}", invoice.InvoiceNumber);
            }
            finally
            {
                emailSemaphore.Release();
            }
        });

        await Task.WhenAll(emailTasks);
    }

    private static TimeZoneInfo GetTimeZoneInfo(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch
        {
            if (CrossPlatformTimeZones.TryGetValue(timeZoneId, out var mapped))
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(mapped);
                }
                catch { }
            }

            return TimeZoneInfo.Utc;
        }
    }
}

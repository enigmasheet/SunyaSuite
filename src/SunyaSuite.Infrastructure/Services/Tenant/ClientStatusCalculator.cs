using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Entities.Tenant;
using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Infrastructure.Services.Tenant;

public class ClientStatusCalculator : IClientStatusCalculator
{
    private const int YellowStatusThresholdDays = 7;
    private readonly TimeProvider _timeProvider;

    public ClientStatusCalculator(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public ClientStatus Calculate(IEnumerable<Invoice> invoices)
    {
        ArgumentNullException.ThrowIfNull(invoices);

        var activeInvoices = invoices.Where(i => !i.IsDeleted);
        if (activeInvoices.Any(i => i.Status == InvoiceStatus.Overdue))
            return ClientStatus.Red;

        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        if (activeInvoices.Any(i => i.Status == InvoiceStatus.Sent
                                 && i.DueDate <= today.AddDays(YellowStatusThresholdDays)))
            return ClientStatus.Yellow;

        return ClientStatus.Green;
    }
}

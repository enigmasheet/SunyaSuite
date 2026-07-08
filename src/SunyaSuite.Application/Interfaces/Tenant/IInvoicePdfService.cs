using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Application.Interfaces.Tenant;

public interface IInvoicePdfService
{
    Task<byte[]> GeneratePdfAsync(InvoiceDetailDto invoice, CopyType copyType = CopyType.Original, DateDisplayPreference preference = DateDisplayPreference.Gregorian, CancellationToken ct = default);
}

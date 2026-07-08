using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Domain.Enums;

namespace SunyaSuite.Application.Interfaces.Tenant;

public interface IReceiptPdfService
{
    Task<byte[]> GeneratePdfAsync(MoneyReceiptDetailDto receipt, CopyType copyType = CopyType.Original, DateDisplayPreference preference = DateDisplayPreference.Gregorian, CancellationToken ct = default);
}

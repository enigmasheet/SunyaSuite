using SunyaSuite.Application.DTOs.Tenant;
using SunyaSuite.Application.Interfaces.Tenant;
using SunyaSuite.Domain.Enums;
using System.Net.Http.Json;

namespace SunyaSuite.Web.Client.Services;

public class ReceiptPdfServiceClient : IReceiptPdfService
{
    private readonly HttpClient _http;

    public ReceiptPdfServiceClient(HttpClient http) => _http = http;

    public async Task<byte[]> GeneratePdfAsync(MoneyReceiptDetailDto receipt, CopyType copyType = CopyType.Original, DateDisplayPreference preference = DateDisplayPreference.Gregorian, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync($"{ApiEndpoints.ReceiptPdf}/generate", new { receiptId = receipt.Id, copyType, preference }, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }
}

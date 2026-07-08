using SunyaSuite.Application.Interfaces;

namespace SunyaSuite.Web.Client.Services;

/// <summary>
/// Minimal client-side implementation of Nepali date logic.
/// For full accuracy, this should call the API or include the Nepali date conversion library.
/// </summary>
public class NepaliDateServiceClient : INepaliDateService
{
    public string ToNepaliDateString(DateTime gregorian, string format = "yyyy-MM-dd")
    {
        // For now, fall back to Gregorian display. The API should be called for accurate Nepali dates.
        return gregorian.ToString(format);
    }

    public string GetFiscalYear(DateTime gregorian)
    {
        // Simplified fiscal year calculation (Nepali FY starts mid-April)
        var year = gregorian.Year;
        return gregorian.Month >= 4 ? $"{year}/{year + 1}" : $"{year - 1}/{year}";
    }
}

using NepDate;
using SunyaSuite.Application.Interfaces;

namespace SunyaSuite.Infrastructure.Services;

public class NepaliDateService : INepaliDateService
{
    public string ToNepaliDateString(DateTime gregorian, string format = "yyyy-MM-dd")
    {
        var nepali = new NepaliDate(gregorian);
        return nepali.ToString(format);
    }

    public string GetFiscalYear(DateTime gregorian)
    {
        var nepali = new NepaliDate(gregorian);
        var fyYear = nepali.Month >= 4 ? nepali.Year : nepali.Year - 1;
        var fyRange = NepaliDateRange.ForFiscalYear(fyYear);
        var endYear = fyRange.End.Year % 100;
        return $"{fyYear}/{endYear:D2}";
    }
}

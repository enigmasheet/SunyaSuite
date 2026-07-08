namespace SunyaSuite.Application.Interfaces;

public interface INepaliDateService
{
    string ToNepaliDateString(DateTime gregorian, string format = "yyyy-MM-dd");
    string GetFiscalYear(DateTime gregorian);
}

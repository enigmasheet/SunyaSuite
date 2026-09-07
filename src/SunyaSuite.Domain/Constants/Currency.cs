namespace SunyaSuite.Domain.Constants;

public static class Currency
{
    public const string Symbol = "Rs.";

    public static string Format(decimal amount) => $"{Symbol} {amount:N2}";
    public static string FormatCompact(decimal amount) => $"{Symbol} {amount:N0}";
}

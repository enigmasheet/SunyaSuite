namespace SunyaSuite.Domain.Constants;

public static class VatDefaults
{
    public const decimal DefaultRate = 13m;

    public static string FormatLabel(decimal rate) => $"VAT ({rate}%):";
}

namespace SunyaSuite.Application.Settings;

public class VatSettings
{
    public const string SectionName = "Vat";

    public decimal Rate { get; set; } = 13m;
}

using NumericWordsConversion;
using SunyaSuite.Application.Interfaces;

namespace SunyaSuite.Infrastructure.Services;

public class NumberToWordsService : INumberToWordsService
{
    private readonly CurrencyWordsConverter _converter;

    public NumberToWordsService()
    {
        _converter = new CurrencyWordsConverter(new CurrencyWordsConversionOptions
        {
            Culture = Culture.Nepali,
            OutputFormat = OutputFormat.English,
            CurrencyUnit = "rupee",
            SubCurrencyUnit = "paisa",
            EndOfWordsMarker = "only"
        });
    }

    public string ToNepaliWords(decimal amount)
    {
        return _converter.ToWords(amount);
    }
}

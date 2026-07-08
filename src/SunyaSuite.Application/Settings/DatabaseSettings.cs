namespace SunyaSuite.Application.Settings;

public class DatabaseSettings
{
    public const string SectionName = "ConnectionStrings";

    public string ConfigConnection { get; set; } = string.Empty;
    public string TemplateConnection { get; set; } = string.Empty;
}

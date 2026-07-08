namespace SunyaSuite.Application.Settings;

public class InviteSettings
{
    public const string SectionName = "InviteSettings";
    public int DefaultExpirationHours { get; set; } = 168; // 7 days
}

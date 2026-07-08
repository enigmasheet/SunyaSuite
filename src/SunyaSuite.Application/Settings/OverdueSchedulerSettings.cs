namespace SunyaSuite.Application.Settings;

public class OverdueSchedulerSettings
{
    public const string SectionName = "OverdueScheduler";

    public int RunHour { get; set; } = 9;
    public int RunMinute { get; set; } = 5;
    public string TimeZone { get; set; } = "India Standard Time";
    public bool Enabled { get; set; } = true;
}

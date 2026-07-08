using System.Text.Json.Serialization;

namespace SunyaSuite.Application.Settings;

public class EmailSettings
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    [JsonIgnore]
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "noreply@sunya.local";
    public string FromName { get; set; } = "SunyaSuite";
}

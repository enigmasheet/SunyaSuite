using System.ComponentModel.DataAnnotations;

namespace SunyaSuite.Application.Settings;

public class JwtSettings
{
    public const string SectionName = "Jwt";

    [Required, MinLength(32, ErrorMessage = "JWT Secret must be at least 32 characters.")]
    public string Secret { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string Issuer { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string Audience { get; set; } = string.Empty;

    [Range(1, 1440, ErrorMessage = "Access token expiry must be between 1 and 1440 minutes.")]
    public int ExpirationInMinutes { get; set; } = 15;

    [Range(1, 90, ErrorMessage = "Refresh token expiry must be between 1 and 90 days.")]
    public int RefreshTokenExpirationInDays { get; set; } = 7;
}

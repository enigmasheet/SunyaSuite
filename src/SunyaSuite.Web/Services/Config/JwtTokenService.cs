using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SunyaSuite.Application.Settings;
using SunyaSuite.Domain.Constants;
using SunyaSuite.Domain.Entities.Config;
using SunyaSuite.Infrastructure.Data.Config;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SunyaSuite.Web.Api.Services.Config;

public class JwtTokenService
{
    private readonly JwtSettings _settings;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TimeProvider _timeProvider;
    private readonly IDbContextFactory<ConfigDbContext> _configFactory;

    public JwtTokenService(
        IOptions<JwtSettings> settings,
        UserManager<ApplicationUser> userManager,
        TimeProvider timeProvider,
        IDbContextFactory<ConfigDbContext> configFactory)
    {
        _settings = settings.Value;
        _userManager = userManager;
        _timeProvider = timeProvider;
        _configFactory = configFactory;
    }

    public async Task<(string token, DateTime expiresAt)> GenerateTokenAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? ""),
            new(ClaimTypes.Email, user.Email ?? "")
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        await using var configDb = await _configFactory.CreateDbContextAsync();
        var orgRoleData = await configDb.OrganizationUsers
            .AsNoTracking()
            .Where(ou => ou.UserId == user.Id)
            .Select(ou => new { ou.OrganizationId, ou.Role })
            .ToListAsync();

        foreach (var data in orgRoleData)
            claims.Add(new Claim(ClaimNames.OrgRole, $"{data.OrganizationId}:{data.Role}"));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var expiresAt = _timeProvider.GetUtcNow().UtcDateTime.AddMinutes(_settings.ExpirationInMinutes);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public ClaimsPrincipal? ValidateToken(string token, bool validateLifetime = true)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var handler = new JwtSecurityTokenHandler();

        try
        {
            return handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = validateLifetime,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _settings.Issuer,
                ValidAudience = _settings.Audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.FromMinutes(5)
            }, out _);
        }
        catch
        {
            return null;
        }
    }
}

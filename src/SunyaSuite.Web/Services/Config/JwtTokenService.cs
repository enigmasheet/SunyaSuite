using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SunyaSuite.Application.Settings;
using SunyaSuite.Domain.Constants;
using SunyaSuite.Domain.Entities.Config;
using SunyaSuite.Infrastructure.Data.Config;

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
        IDbContextFactory<ConfigDbContext> configFactory
    )
    {
        _settings = settings.Value;
        _userManager = userManager;
        _timeProvider = timeProvider;
        _configFactory = configFactory;
    }

    public async Task<(string token, DateTime expiresAt)> GenerateAccessTokenAsync(
        ApplicationUser user
    )
    {
        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? ""),
            new(ClaimTypes.Email, user.Email ?? ""),
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        await using var configDb = await _configFactory.CreateDbContextAsync();
        var orgRoleData = await configDb
            .OrganizationUsers.AsNoTracking()
            .Where(ou => ou.UserId == user.Id)
            .Select(ou => new { ou.OrganizationId, ou.Role })
            .ToListAsync();

        foreach (var data in orgRoleData)
            claims.Add(new Claim(ClaimNames.OrgRole, $"{data.OrganizationId}:{data.Role}"));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var expiresAt = _timeProvider
            .GetUtcNow()
            .UtcDateTime.AddMinutes(_settings.ExpirationInMinutes);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    [Obsolete(
        "Use GenerateAccessTokenAsync instead. This method exists for backward compatibility during migration."
    )]
    public Task<(string token, DateTime expiresAt)> GenerateTokenAsync(ApplicationUser user) =>
        GenerateAccessTokenAsync(user);

    /// <summary>
    /// Generates a new refresh token. Returns the raw token string (for the client)
    /// and stores only its SHA-256 hash in the database.
    /// </summary>
    public async Task<(RefreshToken entity, string rawToken)> GenerateRefreshTokenAsync(
        ApplicationUser user,
        string? createdByIp = null
    )
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashTokenString(rawToken),
            CreatedByIp = createdByIp,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiresAt = _timeProvider
                .GetUtcNow()
                .UtcDateTime.AddDays(_settings.RefreshTokenExpirationInDays),
        };

        await using var configDb = await _configFactory.CreateDbContextAsync();
        configDb.RefreshTokens.Add(refreshToken);
        await configDb.SaveChangesAsync();

        return (refreshToken, rawToken);
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string tokenHash)
    {
        await using var configDb = await _configFactory.CreateDbContextAsync();
        return await configDb.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
    }

    /// <summary>
    /// Rotates a refresh token. Returns the new raw token string (for the client)
    /// and stores only its hash. The old token is revoked.
    /// </summary>
    public async Task<(RefreshToken entity, string rawToken)?> RotateRefreshTokenAsync(
        RefreshToken oldRefreshToken,
        string? createdByIp = null
    )
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = oldRefreshToken.UserId,
            TokenHash = HashTokenString(rawToken),
            CreatedByIp = createdByIp,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiresAt = _timeProvider
                .GetUtcNow()
                .UtcDateTime.AddDays(_settings.RefreshTokenExpirationInDays),
        };

        await using var configDb = await _configFactory.CreateDbContextAsync();

        oldRefreshToken.RevokedAt = _timeProvider.GetUtcNow().UtcDateTime;
        oldRefreshToken.ReplacedByTokenHash = newRefreshToken.TokenHash;
        oldRefreshToken.ReasonRevoked = "Replaced by new token";

        configDb.RefreshTokens.Update(oldRefreshToken);
        configDb.RefreshTokens.Add(newRefreshToken);
        await configDb.SaveChangesAsync();

        return (newRefreshToken, rawToken);
    }

    /// <summary>
    /// Revokes all non-revoked refresh tokens for a user.
    /// </summary>
    public async Task RevokeRefreshTokensByUserIdAsync(string userId, string reason)
    {
        await using var configDb = await _configFactory.CreateDbContextAsync();
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var tokensToRevoke = await configDb
            .RefreshTokens.Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var t in tokensToRevoke)
        {
            t.RevokedAt = now;
            t.ReasonRevoked = reason;
        }

        await configDb.SaveChangesAsync();
    }

    /// <summary>
    /// Revokes all non-revoked refresh tokens for the user who owns the given token hash.
    /// Used for token-reuse detection.
    /// </summary>
    public async Task RevokeRefreshTokenFamilyAsync(string tokenHash, string reason)
    {
        await using var configDb = await _configFactory.CreateDbContextAsync();
        var token = await configDb.RefreshTokens.FirstOrDefaultAsync(rt =>
            rt.TokenHash == tokenHash
        );
        if (token is null)
            return;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var tokensToRevoke = await configDb
            .RefreshTokens.Where(rt => rt.UserId == token.UserId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var t in tokensToRevoke)
        {
            t.RevokedAt = now;
            t.ReasonRevoked = reason;
        }

        await configDb.SaveChangesAsync();
    }

    public ClaimsPrincipal? ValidateToken(string token, bool validateLifetime = true)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var handler = new JwtSecurityTokenHandler();

        try
        {
            return handler.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = validateLifetime,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _settings.Issuer,
                    ValidAudience = _settings.Audience,
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.Zero,
                },
                out _
            );
        }
        catch
        {
            return null;
        }
    }

    public static string HashTokenString(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))).ToLowerInvariant();
}

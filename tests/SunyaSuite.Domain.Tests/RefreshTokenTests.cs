using FluentAssertions;
using SunyaSuite.Domain.Entities.Config;
using Xunit;

namespace SunyaSuite.Domain.Tests;

public class RefreshTokenTests
{
    [Fact]
    public void IsActive_WhenNotExpiredAndNotRevoked_ReturnsTrue()
    {
        var token = new RefreshToken { ExpiresAt = DateTime.UtcNow.AddHours(1), RevokedAt = null };

        token.IsActive.Should().BeTrue();
        token.IsExpired.Should().BeFalse();
        token.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenPastExpiry_ReturnsTrue()
    {
        var token = new RefreshToken { ExpiresAt = DateTime.UtcNow.AddHours(-1), RevokedAt = null };

        token.IsExpired.Should().BeTrue();
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsRevoked_WhenRevokedAtHasValue_ReturnsTrue()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            RevokedAt = DateTime.UtcNow,
        };

        token.IsRevoked.Should().BeTrue();
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WhenExpiredAndRevoked_ReturnsFalse()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            RevokedAt = DateTime.UtcNow.AddHours(-0.5),
        };

        token.IsActive.Should().BeFalse();
    }
}

using FastMCP.Authentication.Core;
using Xunit;

namespace FastMCP.Tests.Authentication;

public class AccessTokenTests
{
    [Fact]
    public void AccessToken_NotExpired_ReturnsIsExpiredFalse()
    {
        var token = new AccessToken
        {
            Token = "valid_token",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
        };

        Assert.False(token.IsExpired);
    }

    [Fact]
    public void AccessToken_Expired_ReturnsIsExpiredTrue()
    {
        var token = new AccessToken
        {
            Token = "expired_token",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds()
        };

        Assert.True(token.IsExpired);
    }

    [Fact]
    public void AccessToken_NullExpiresAt_NeverExpires()
    {
        var token = new AccessToken
        {
            Token = "non_expiring_token",
            ExpiresAt = null
        };

        Assert.False(token.IsExpired);
    }

    [Fact]
    public void HasRequiredScopes_AllPresent_ReturnsTrue()
    {
        var token = new AccessToken
        {
            Scopes = new[] { "read:tools", "write:tools", "admin" }
        };

        Assert.True(token.HasRequiredScopes(new[] { "read:tools", "admin" }));
    }

    [Fact]
    public void HasRequiredScopes_MissingScope_ReturnsFalse()
    {
        var token = new AccessToken
        {
            Scopes = new[] { "read:tools" }
        };

        Assert.False(token.HasRequiredScopes(new[] { "read:tools", "write:tools" }));
    }

    [Fact]
    public void HasRequiredScopes_EmptyRequired_ReturnsTrue()
    {
        var token = new AccessToken
        {
            Scopes = new[] { "read:tools" }
        };

        Assert.True(token.HasRequiredScopes(Array.Empty<string>()));
    }
}

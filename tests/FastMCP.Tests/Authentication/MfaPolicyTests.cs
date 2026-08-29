using System.Security.Claims;
using FastMCP.Hosting;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace FastMCP.Tests.Authentication;

public class MfaPolicyTests
{
    [Fact]
    public async Task AuthenticatedUser_SucceedsPolicyRequirement()
    {
        var handler = new PolicyNameRequirementHandler();
        var requirement = new PolicyNameRequirement("AdminOnly");
        
        var claims = new[] { new Claim(ClaimTypes.Name, "test_user") };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var user = new ClaimsPrincipal(identity);

        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);
        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task UnauthenticatedUser_FailsPolicyRequirement()
    {
        var handler = new PolicyNameRequirementHandler();
        var requirement = new PolicyNameRequirement("AdminOnly");
        var user = new ClaimsPrincipal(new ClaimsIdentity()); // Unauthenticated

        var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);
        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}

using FastMCP.Attributes;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AzureAdOAuthServer.Tools;

public static class Tools
{
    /// <summary>
    /// Echo tool - demonstrates basic authenticated tool access.
    /// </summary>
    [McpTool]
    [Authorize]
    public static Task<object> Echo(string message)
    {
        return Task.FromResult<object>(new
        {
            message = $"Echo: {message}",
            authenticated = true,
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get user info - demonstrates accessing authenticated user claims.
    /// </summary>
    [McpTool]
    [Authorize]
    public static Task<object> GetUserInfo(ClaimsPrincipal user)
    {
        return Task.FromResult<object>(new
        {
            name = user.Identity?.Name ?? "Unknown",
            isAuthenticated = user.Identity?.IsAuthenticated ?? false,
            authenticationType = user.Identity?.AuthenticationType ?? "None",
            claims = user.Claims.Select(c => new { type = c.Type, value = c.Value }).ToList()
        });
    }

    /// <summary>
    /// Get provider-specific info - demonstrates accessing Azure AD claims.
    /// </summary>
    [McpTool]
    [Authorize]
    public static Task<object> GetProviderSpecificInfo(ClaimsPrincipal user)
    {
        return Task.FromResult<object>(new
        {
            email = user.FindFirst(ClaimTypes.Email)?.Value ?? 
                   user.FindFirst("email")?.Value ?? 
                   user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value ?? 
                   "Not available",
            
            subject = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                     user.FindFirst("sub")?.Value ?? 
                     "Not available",
            
            // Azure AD specific claims
            objectId = user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value ?? "Not available",
            tenantId = user.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value ?? "Not available",
            preferredUsername = user.FindFirst("preferred_username")?.Value ?? "Not available"
        });
    }

    /// <summary>
    /// Public tool - no authentication required.
    /// </summary>
    [McpTool]
    public static Task<object> PublicEcho(string message)
{
    return Task.FromResult<object>(new
    {
        message = $"Public Echo: {message}",
        echoed = true,
        authenticated = false,
        timestamp = DateTime.UtcNow
    });
}
}
using FastMCP.Attributes;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AwsCognitoOAuthServer.Tools;

public static class Tools
{
    /// <summary>
    /// Echo tool - demonstrates basic authenticated tool access.
    /// </summary>
    [McpTool]
    [Authorize]
    public static string Echo(string message)
    {
        return $"Echo: {message}";
    }

    /// <summary>
    /// Get user info - demonstrates accessing authenticated user claims.
    /// </summary>
    [McpTool]
    [Authorize]
    public static Dictionary<string, string> GetUserInfo(ClaimsPrincipal user)
    {
        return new Dictionary<string, string>
        {
            { "Name", user.Identity?.Name ?? "Unknown" },
            { "IsAuthenticated", user.Identity?.IsAuthenticated.ToString() ?? "False" },
            { "AuthenticationType", user.Identity?.AuthenticationType ?? "None" },
            { "Claims", string.Join(", ", user.Claims.Select(c => $"{c.Type}={c.Value}")) }
        };
    }

    /// <summary>
    /// Get provider-specific info - demonstrates accessing AWS Cognito claims.
    /// </summary>
    [McpTool]
    [Authorize]
    public static Dictionary<string, string> GetProviderSpecificInfo(ClaimsPrincipal user)
    {
        var info = new Dictionary<string, string>();
        
        info["Email"] = user.FindFirst(ClaimTypes.Email)?.Value ?? 
                       user.FindFirst("email")?.Value ?? 
                       user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value ?? 
                       "Not available";
        
        info["Subject"] = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                         user.FindFirst("sub")?.Value ?? 
                         "Not available";
        
        // AWS Cognito specific claims examples
        info["Cognito Username"] = user.FindFirst("cognito:username")?.Value ?? "Not available";
        info["Given Name"] = user.FindFirst(ClaimTypes.GivenName)?.Value ?? "Not available";
        info["Family Name"] = user.FindFirst(ClaimTypes.Surname)?.Value ?? "Not available";
        
        return info;
    }

    /// <summary>
    /// Public tool - no authentication required.
    /// </summary>
    [McpTool]
    public static string PublicEcho(string message)
    {
        return $"Public Echo: {message}";
    }
}
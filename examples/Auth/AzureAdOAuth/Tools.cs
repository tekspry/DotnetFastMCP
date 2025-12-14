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
    /// Get provider-specific info - demonstrates accessing Azure AD claims.
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
        
        // Azure AD specific claims examples
        info["Object ID"] = user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value ?? "Not available";
        info["Tenant ID"] = user.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value ?? "Not available";
        info["Preferred Username"] = user.FindFirst("preferred_username")?.Value ?? "Not available";
        
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
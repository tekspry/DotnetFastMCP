using FastMCP.Hosting;
using FastMCP.Server;
using System.Reflection;
using OktaOAuthServer.Tools;

try
{
    Console.WriteLine("[OktaOAuthServer] Starting...");
    
    var mcpServer = new FastMCPServer(name: "Okta OAuth Example Server");
    var builder = McpServerBuilder.Create(mcpServer, args);
    
    // Configure Okta OAuth using OAuth Proxy
    builder.AddOktaOAuthProxy();
    
    builder.WithComponentsFrom(Assembly.GetExecutingAssembly());
    Console.WriteLine($"[OktaOAuthServer] Registered {mcpServer.Tools.Count} tools and {mcpServer.Resources.Count} resources");

    var app = builder.Build();
    app.Urls.Add("http://localhost:5005");
    
    Console.WriteLine("[OktaOAuthServer] Server starting on http://localhost:5005");
    Console.WriteLine("[OktaOAuthServer] MCP endpoint: http://localhost:5005/mcp");
    Console.WriteLine("[OktaOAuthServer] OAuth callback: http://localhost:5005/auth/callback");
    Console.Out.Flush();
    
    await app.RunAsync();
    
    Console.WriteLine("[OktaOAuthServer] Server stopped");
}
catch (OperationCanceledException)
{
    Console.WriteLine("[OktaOAuthServer] Server was cancelled");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[OktaOAuthServer] FATAL: {ex.Message}");
    Console.Error.WriteLine($"[OktaOAuthServer] Stack: {ex. 
        info["Email"] = user.FindFirst(ClaimTypes.Email)?.Value ?? 
                       user.FindFirst("email")?.Value ?? 
                       user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value ?? 
                       "Not available";
        
        info["Subject"] = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                         user.FindFirst("sub")?.Value ?? 
                         "Not available";
        
        // Okta specific claims examples
        info["Preferred Username"] = user.FindFirst("preferred_username")?.Value ?? "Not available";
        info["Locale"] = user.FindFirst("locale")?.Value ?? "Not available";
        info["Zoneinfo"] = user.FindFirst("zoneinfo")?.Value ?? "Not available";
        
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
using FastMCP.Hosting;
using FastMCP.Server;
using System.Reflection;
using AzureAdOAuthServer.Tools;

try
{
    Console.WriteLine("[AzureAdOAuthServer] Starting...");
    
    var mcpServer = new FastMCPServer(name: "Azure AD OAuth Example Server");
    var builder = McpServerBuilder.Create(mcpServer, args);
    
    // DEBUG: Check if environment variables are visible
    var tenantId = Environment.GetEnvironmentVariable("FASTMCP_SERVER_AUTH_AZUREAD_TENANT_ID");
    var clientId = Environment.GetEnvironmentVariable("FASTMCP_SERVER_AUTH_AZUREAD_CLIENT_ID");
    Console.WriteLine($"[DEBUG] TenantID present: {!string.IsNullOrEmpty(tenantId)}");
    Console.WriteLine($"[DEBUG] ClientID present: {!string.IsNullOrEmpty(clientId)}");
    
    // Configure Azure AD authentication using token verifier
    builder.AddAzureAdTokenVerifier();
    
    builder.WithComponentsFrom(Assembly.GetExecutingAssembly());
    Console.WriteLine($"[AzureAdOAuthServer] Registered {mcpServer.Tools.Count} tools");

    var app = builder.Build();
    app.Urls.Add("http://localhost:5002");
    
    Console.WriteLine("[AzureAdOAuthServer] Server starting on http://localhost:5002");
    Console.WriteLine("[AzureAdOAuthServer] MCP endpoint: http://localhost:5002/mcp");
    Console.Out.Flush();
    
    await app.RunAsync();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[AzureAdOAuthServer] FATAL: {ex.Message}");
    Console.Error.WriteLine($"[AzureAdOAuthServer] Stack: {ex.StackTrace}");
    Environment.Exit(1);
}
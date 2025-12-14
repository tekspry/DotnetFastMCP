using FastMCP.Hosting;
using FastMCP.Server;
using System.Reflection;
using AzureAdOAuthServer.Tools;

try
{
    Console.WriteLine("[AzureAdOAuthServer] Starting...");
    
    var mcpServer = new FastMCPServer(name: "Azure AD OAuth Example Server");
    var builder = McpServerBuilder.Create(mcpServer, args);
    
    // Configure Azure AD authentication using token verifier
    builder.AddAzureAd();
    
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
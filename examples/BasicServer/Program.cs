using FastMCP.Hosting;
using FastMCP.Server;
using System.Reflection;
using BasicServer.Tools;

try
{
    Console.WriteLine("[BasicServer] Starting...");
    
    var mcpServer = new FastMCPServer(name: "My First Dotnet MCP Server");
    var builder = McpServerBuilder.Create(mcpServer, args);
    builder.WithComponentsFrom(Assembly.GetExecutingAssembly());
    Console.WriteLine($"[BasicServer] Registered {mcpServer.Tools.Count} tools and {mcpServer.Resources.Count} resources");

    var app = builder.Build();
    Console.WriteLine("[BasicServer] App built, starting to run...");
    Console.Out.Flush();
    
    await app.RunAsync();
    
    Console.WriteLine("[BasicServer] App finished running");
}
catch (OperationCanceledException)
{
    Console.WriteLine("[BasicServer] App was cancelled");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[BasicServer] FATAL: {ex.Message}");
    Console.Error.WriteLine($"[BasicServer] Stack: {ex.StackTrace}");
    Environment.Exit(1);
}

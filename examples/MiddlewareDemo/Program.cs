using FastMCP.Attributes;
using FastMCP.Hosting;
using FastMCP.Server;
using FastMCP.Protocol;
using System.Reflection;
using System.Text.Json;
using MiddlewareDemo;

// 1. Define Middleware
// This middleware logs every request and response to Console.Error (Stderr)
// Stderr is safe to use even in Stdio mode (unlike Console.Out)




// --- CLIENT MODE ---
if (args.Contains("--client"))
{
    Console.WriteLine("🚀 Starting MiddlewareDemo Client...");
    
    // We run OURSELVES as the server in Stdio mode
    var dllPath = Assembly.GetExecutingAssembly().Location;
    
    // 1. Create Client
    var transport = new FastMCP.Client.Transports.StdioClientTransport("dotnet", $"{dllPath} --stdio");
    await using var client = new FastMCP.Client.McpClient(transport);

    // 2. Connect
    await client.ConnectAsync();
    Console.WriteLine("✅ Connected to self!");

    // 3. Call 'Add' Tool
    // This should trigger the Middleware LOGS (in stderr)
    var result = await client.CallToolAsync<int>("Add", new { a = 100, b = 200 });
    Console.WriteLine($"Result: {result}");

    return;
}

// --- SERVER MODE ---

// 3. Setup Server
var server = new FastMCPServer("MiddlewareDemo");
var builder = McpServerBuilder.Create(server, args);

// Register our middleware
builder.AddMcpMiddleware<LoggingMiddleware>();

// Register tools
builder.WithComponentsFrom(Assembly.GetExecutingAssembly());

var app = builder.Build();

// Run
// await app.RunAsync(); // OLD: Only runs HTTP
await app.RunMcpAsync(args); // NEW: Supports --stdio switch

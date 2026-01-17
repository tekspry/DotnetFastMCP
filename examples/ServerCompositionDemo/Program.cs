using FastMCP.Attributes;
using FastMCP.Hosting;
using FastMCP.Server;
using System.Reflection;

// 1. Create Main Server
var mainServer = new FastMCPServer("MainServer");
var builder = McpServerBuilder.Create(mainServer, args);

// 2. Create Sub Server (Hidden implementation)
var subServer = new FastMCPServer("GitHubTools");
// Register tools manually or via scanning another assembly
// Here we scan a static class "SubServerTools" manually
foreach (var method in typeof(SubServerTools).GetMethods())
{
    if (method.GetCustomAttribute<McpToolAttribute>() != null)
    {
        var attr = method.GetCustomAttribute<McpToolAttribute>();
        subServer.Tools.TryAdd(attr?.Name ?? method.Name, method);
    }
}

// 3. Import Sub Server with Prefix
builder.AddServer(subServer, prefix: "gh");

// 4. Register Main Server Tools
builder.WithComponentsFrom(Assembly.GetExecutingAssembly());

var app = builder.Build();

// Run
await app.RunMcpAsync(args);

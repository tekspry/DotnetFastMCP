using FastMCP.Attributes;
using FastMCP.Hosting;
using FastMCP.Server;
using FastMCP;
using System.Reflection;

// 1. Create Server
var server = new FastMCPServer("StorageDemo");
var builder = McpServerBuilder.Create(server, args);

// 2. Register Tools
builder.WithComponentsFrom(Assembly.GetExecutingAssembly());

// 3. Build & Run
var app = builder.Build();
await app.RunMcpAsync(args);

public static class StorageTools
{
    [McpTool]
    public static async Task<string> StoreValue(string key, string value, McpContext context)
    {
        await context.Storage.SetAsync(key, value);
        return $"Stored '{value}' in '{key}'";
    }

    [McpTool]
    public static async Task<string> GetValue(string key, McpContext context)
    {
        var value = await context.Storage.GetAsync<string>(key);
        return value ?? "Not found";
    }

    [McpTool]
    public static async Task<string> DeleteValue(string key, McpContext context)
    {
        await context.Storage.DeleteAsync(key);
        return $"Deleted key '{key}'";
    }
}

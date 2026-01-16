using FastMCP.Client;
using FastMCP.Client.Transports;

Console.WriteLine("🚀 Starting MCP Client Demo...");

// Path to the BasicServer implementation
// We assume it's built and available relative to this project
var serverDllPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../BasicServer/bin/Debug/net8.0/BasicServer.dll"));

if (!File.Exists(serverDllPath))
{
    Console.WriteLine($"❌ Could not find BasicServer at: {serverDllPath}");
    Console.WriteLine("Please build the BasicServer project first.");
    return;
}

Console.WriteLine($"🔌 Connecting to server at: {serverDllPath}");

// Create the transport (Stdio)
// We use 'dotnet' to run the server DLL with the '--stdio' flag
var transport = new StdioClientTransport("dotnet", $"{serverDllPath} --stdio");

// create the client
await using var client = new McpClient(transport);

// Subscribe to logs/notifications
client.OnNotification += (method, payload) =>
{
    Console.WriteLine($"[NOTIFY] {method}: {payload}");
};

try
{
    await client.ConnectAsync();
    Console.WriteLine("✅ Connected!");

    // 1. List Tools
    Console.WriteLine("\n--- Tools ---");
    var tools = await client.ListToolsAsync();
    foreach (var tool in tools.Tools)
    {
        Console.WriteLine($" - {tool.Name}: {tool.Description}");
    }

    // 2. Call a Tool
    Console.WriteLine("\n--- Calling 'add_numbers' ---");
    var result = await client.CallToolAsync<int>("add_numbers", new { a = 10, b = 55 });
    Console.WriteLine($"Result: 10 + 55 = {result}");

    // 3. Call a Tool that logs (to see notifications)
    Console.WriteLine("\n--- Calling 'TestContext' (Wait for logs...) ---");
    // Note: Assuming 'TestContext' exists and emits logs
    // We construct the parameters anonymously
    var contextResult = await client.CallToolAsync<string>("TestContext", new { input = "Hello Client!" });
    Console.WriteLine($"Context Result: {contextResult}");

}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}

Console.WriteLine("\nDemo finished. Press any key to exit.");
Console.ReadKey();
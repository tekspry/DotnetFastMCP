using FastMCP.Attributes;
using FastMCP.Hosting;
using FastMCP.Server;
using FastMCP;
using System.Reflection;

// 1. Create Server
var server = new FastMCPServer("BackgroundDemo");
var builder = McpServerBuilder.Create(server, args);

// 2. Register Tools
builder.WithComponentsFrom(Assembly.GetExecutingAssembly());

// 3. Register Background Services
// (Usually automatic if you use the builder extensions, assuming McpServerBuilder registers them by default now)
// But our design spec said McpServerBuilder constructor registers them.

// 4. Build & Run
var app = builder.Build();
await app.RunMcpAsync(args);

public static class BackgroundTools
{
    [McpTool]
    public static async Task<string> StartJob(string name, McpContext context)
    {
        await context.RunInBackground(async (ct) => 
        {
            await Task.Delay(1000, ct); // Simulate work
            Console.Error.WriteLine($"[Background] Job '{name}' completed successfully.");
        });

        return $"Job '{name}' accepted.";
    }

    [McpTool]
    public static async Task<string> StartHeavyJob(int durationMs, McpContext context)
    {
        Console.Error.WriteLine($"[Main] Queuing heavy job for {durationMs}ms...");
        
        await context.RunInBackground(async (ct) => 
        {
            Console.Error.WriteLine($"[Background] Heavy job started (Duration: {durationMs}ms)");
            await Task.Delay(durationMs, ct);
            Console.Error.WriteLine($"[Background] Heavy job finished.");
        });

        return "Heavy job queued.";
    }

    [McpTool]
    public static async Task<string> StartFailingJob(string reason, McpContext context)
    {
        await context.RunInBackground(async (ct) => 
        {
            await Task.Delay(100, ct);
            Console.Error.WriteLine($"[Background] About to fail with: {reason}");
            throw new InvalidOperationException($"Simulated failure: {reason}");
        });

        return "Failing job queued (watch console for error).";
    }
}

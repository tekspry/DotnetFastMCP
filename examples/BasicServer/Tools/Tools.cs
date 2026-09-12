using FastMCP;
using FastMCP.Attributes;
using Microsoft.Extensions.Logging;

namespace BasicServer.Tools;

// ─────────────────────────────────────────────────────────────────────────────
// Static tools (no DI needed) — the classic approach, unchanged.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// General tools for the MCP server.
/// </summary>
public static class Tools
{
    /// <summary>
    /// Adds two integer numbers together.
    /// </summary>
    [McpTool("add_numbers", Description = "Adds two integers and returns their sum")]
    public static int Add(
        [McpDescription("The first integer operand")] int a,
        [McpDescription("The second integer operand")] int b)
    {
        return a + b;
    }

    [McpTool(Description = "Processes input with progress reporting via McpContext")]
    public static async Task<string> TestContext(
        [McpDescription("The input string to process")] string input,
        McpContext context)   // McpContext is framework-injected — excluded from schema automatically
    {
        await context.LogInfoAsync($"Received input: {input}");
        await context.ReportProgressAsync(10, 100);
        await Task.Delay(100);
        await context.ReportProgressAsync(100, 100);
        return $"Processed: {input}";
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Instance tool with constructor injection — demonstrates v2.1 auto-DI feature.
//
// Previously: you had to manually call builder.Services.AddTransient<GreetingTool>()
// Now:        WithComponentsFrom() handles it automatically.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// A non-static tool that uses constructor injection.
/// With DotnetFastMCP v2.1+, no manual DI registration is required —
/// <c>builder.WithComponentsFrom(Assembly.GetExecutingAssembly())</c>
/// automatically registers this class as Transient.
/// </summary>
public class GreetingTool
{
    private readonly ILogger<GreetingTool> _logger;

    // Constructor injection works out-of-the-box — no AddTransient<GreetingTool>() needed.
    public GreetingTool(ILogger<GreetingTool> logger)
    {
        _logger = logger;
    }

    [McpTool("greet_user", Description = "Returns a personalised greeting for the given user name")]
    public string Greet(
        [McpDescription("The name of the user to greet, e.g. 'Alice'")] string name,
        [McpDescription("The greeting style: 'formal' or 'casual' (default: casual)")] string style = "casual")
    {
        _logger.LogInformation("Greeting user '{Name}' with style '{Style}'", name, style);

        return style.Equals("formal", StringComparison.OrdinalIgnoreCase)
            ? $"Good day, {name}. How may I assist you today?"
            : $"Hey {name}! What can I do for you?";
    }
}

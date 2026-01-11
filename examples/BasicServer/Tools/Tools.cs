using FastMCP;
using FastMCP.Attributes;
using System.Threading.Tasks;

namespace BasicServer.Tools;

/// <summary>
/// General tools for the MCP server.
/// </summary>
public static class Tools
{
    /// <summary>
    /// Adds two integer numbers together.
    /// </summary>
    /// <param name="a">The first number.</param>
    /// <param name="b">The second number.</param>
    /// <returns>The sum of a and b.</returns>
    [McpTool("add_numbers")]
    public static int Add(int a, int b)
    {
        return a + b;
    }

    [McpTool]
    public static async Task<string> TestContext(string input, McpContext context)
    {
        await context.LogInfoAsync($"Received input: {input}");
        await context.ReportProgressAsync(10, 100);
        await Task.Delay(100); 
        await context.ReportProgressAsync(100, 100);
        return $"Processed: {input}";
    }
}

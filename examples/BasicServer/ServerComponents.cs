using FastMCP.Attributes;

namespace BasicServer.Tools;

/// <summary>
/// A class to contain our MCP components.
/// The framework will discover the attributed methods inside.
/// </summary>
public static class ServerComponents
{
    /// <summary>
    /// Adds two integer numbers together. The XML doc comment is used as the description.
    /// </summary>
    /// <param name="a">The first number.</param>
    /// <param name="b">The second number.</param>
    /// <returns>The sum of a and b.</returns>
    [McpTool]
    public static int Add(int a, int b)
    {
        return a + b;
    }

    /// <summary>
    /// Provides the application's static configuration.
    /// </summary>
    [McpResource("resource://config")]
    public static object GetConfig()
    {
        return new { Version = "1.0.0", Author = "DotnetFastMCP Team" };
    }
}

using FastMCP.Attributes;

namespace MiddlewareDemo;

public static class MyTools
{
    [McpTool]
    public static string Ping(string message) => $"Pong: {message}";

    [McpTool]
    public static int Add(int a, int b) => a + b;
}

using FastMCP.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using FastMCP.Server;

namespace FastMCP.Hosting;
public static class McpServerBuilderExtensions
{
    /// <summary>
    /// Mounts another MCP server instance into the current server.
    /// </summary>
    /// <param name="prefix">Optional prefix for all imported tools (e.g. "github")</param>
    public static McpServerBuilder AddServer(this McpServerBuilder builder, FastMCPServer otherServer, string? prefix = null)
    {
        // Access nested server instance (you might need to expose it or pass it differently)
        // Assuming builder.Server is accessible:
        builder.Server.Import(otherServer, prefix);
        return builder;
    }

    /// <summary>
    /// Configures telemetry (OpenTelemetry) for the MCP server.
    /// </summary>
    public static McpServerBuilder WithTelemetry(this McpServerBuilder builder, Action<McpTelemetryOptions>? configure = null)
    {
        var options = new McpTelemetryOptions();
        configure?.Invoke(options);
        // Register Options
        builder.GetWebAppBuilder().Services.AddSingleton(options);
        // Register Instrumentation
        builder.GetWebAppBuilder().Services.AddSingleton<McpInstrumentation>();
        // Register Middleware
        builder.AddMcpMiddleware<McpTelemetryMiddleware>();
        return builder;
    }
}
using FastMCP.Attributes;
using FastMCP.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FastMCP.Hosting;

/// <summary>
/// A builder for creating and configuring a FastMCP server application.
/// This follows the modern .NET hosting pattern (e.g., WebApplicationBuilder).
/// </summary>
public class McpServerBuilder
{
    private readonly WebApplicationBuilder _webAppBuilder;
    private readonly FastMCPServer _mcpServer;

    private McpServerBuilder(FastMCPServer mcpServer, string[]? args)
    {
        _mcpServer = mcpServer;
        _webAppBuilder = WebApplication.CreateBuilder(args ?? Array.Empty<string>());
        _webAppBuilder.Services.AddSingleton(_mcpServer);
    }

    /// <summary>
    /// Creates a new instance of the McpServerBuilder.
    /// </summary>
    public static McpServerBuilder Create(FastMCPServer server, string[]? args = null)
    {
        return new McpServerBuilder(server, args);
    }

    /// <summary>
    /// Scans the specified assembly for methods decorated with McpTool and McpResource 
    /// attributes and registers them with the server.
    /// </summary>
    public McpServerBuilder WithComponentsFrom(Assembly assembly)
    {
        var methods = assembly.GetTypes().SelectMany(t => t.GetMethods());

        foreach (var method in methods)
        {
            if (method.GetCustomAttribute<McpToolAttribute>() is not null)
            {
                _mcpServer.Tools.Add(method);
            }

            if (method.GetCustomAttribute<McpResourceAttribute>() is not null)
            {
                _mcpServer.Resources.Add(method);
            }
        }
        
        return this;
    }

    /// <summary>
    /// Builds the WebApplication that will host the MCP server.
    /// </summary>
    public WebApplication Build()
    {
        var app = _webAppBuilder.Build();

        // Register the MCP protocol middleware for /mcp endpoints
        app.UseMcpProtocol();

        // Root endpoint returns server metadata
        app.MapGet("/", () => 
            $"MCP Server '{_mcpServer.Name}' is running.\n" +
            $"Registered Tools: {_mcpServer.Tools.Count}\n" +
            $"Registered Resources: {_mcpServer.Resources.Count}");

        return app;
    }
}

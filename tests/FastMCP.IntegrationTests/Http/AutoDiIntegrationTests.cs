using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FastMCP.Attributes;
using FastMCP.Hosting;
using FastMCP.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FastMCP.IntegrationTests.Http;

// ── Test fixtures ─────────────────────────────────────────────────────────────

/// <summary>
/// A non-static tool class that requires constructor injection.
/// Intentionally NOT manually registered — auto-DI must handle it.
/// </summary>
public class InjectedGreetingTool
{
    private readonly ILogger<InjectedGreetingTool> _logger;

    public InjectedGreetingTool(ILogger<InjectedGreetingTool> logger)
    {
        _logger = logger;
    }

    [McpTool("greet", Description = "Returns a greeting")]
    public string Greet(
        [McpDescription("The name to greet")] string name)
    {
        _logger.LogInformation("Greeting {Name}", name);
        return $"Hello, {name}!";
    }
}

/// <summary>
/// A static tool class — verifies that scanning an assembly with mixed static
/// and non-static classes handles both correctly.
/// </summary>
public static class StaticMathTool
{
    [McpTool("multiply", Description = "Multiplies two numbers")]
    public static int Multiply(
        [McpDescription("First operand")] int a,
        [McpDescription("Second operand")] int b) => a * b;
}

// ── Integration Tests ─────────────────────────────────────────────────────────

public class AutoDiIntegrationTests : IAsyncLifetime
{
    private WebApplication? _app;
    private HttpClient? _client;
    private string _serverAddress = "";

    public async Task InitializeAsync()
    {
        var server = new FastMCPServer("AutoDiIntegrationServer");
        var builder = McpServerBuilder.Create(server, new[] { "--urls", "http://127.0.0.1:0" });

        // Scan assembly — must auto-register InjectedGreetingTool without manual registration
        builder.WithComponentsFrom(typeof(InjectedGreetingTool).Assembly);

        _app = builder.Build();
        await _app.StartAsync();

        var feature = _app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>();
        _serverAddress = feature?.Addresses.First() ?? "http://127.0.0.1:5000";

        _client = new HttpClient { BaseAddress = new Uri(_serverAddress) };
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    /// <summary>
    /// The primary scenario: calling a non-static tool that requires constructor
    /// injection must succeed without any manual DI registration by the developer.
    /// This is the exact crash path that Feature 1 fixes.
    /// </summary>
    [Fact]
    public async Task NonStaticToolWithConstructorInjection_CalledViaHttp_SucceedsWithoutManualDiRegistration()
    {
        var requestBody = new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/call",
            @params = new { name = "greet", arguments = new { name = "Alice" } }
        };

        var response = await PostMcpRequestAsync(requestBody);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        // Must have no JSON-RPC error
        Assert.False(doc.RootElement.TryGetProperty("error", out _),
            $"Expected no error but got: {json}");

        // Result content must contain the greeting
        var content = doc.RootElement
            .GetProperty("result")
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        Assert.Equal("Hello, Alice!", content);
    }

    /// <summary>
    /// Static tools in the same assembly must continue to work normally.
    /// This verifies that auto-DI registration of non-static types doesn't
    /// break the existing static-method invocation path.
    /// </summary>
    [Fact]
    public async Task StaticToolInMixedAssembly_CalledViaHttp_Succeeds()
    {
        var requestBody = new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/call",
            @params = new { name = "multiply", arguments = new { a = 6, b = 7 } }
        };

        var response = await PostMcpRequestAsync(requestBody);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.False(doc.RootElement.TryGetProperty("error", out _),
            $"Expected no error but got: {json}");

        var content = doc.RootElement
            .GetProperty("result")
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        Assert.Equal("42", content);
    }

    /// <summary>
    /// tools/list must return parameter descriptions from [McpDescription] attributes
    /// and must not include framework-injected parameters (ClaimsPrincipal, McpContext,
    /// CancellationToken) in the schema.
    /// </summary>
    [Fact]
    public async Task ToolsList_ReturnsDescriptionsAndExcludesInjectedParams()
    {
        var requestBody = new
        {
            jsonrpc = "2.0",
            id = 3,
            method = "tools/list"
        };

        var response = await PostMcpRequestAsync(requestBody);
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var tools = doc.RootElement.GetProperty("result").GetProperty("tools");
        var greetTool = tools.EnumerateArray()
            .FirstOrDefault(t => t.GetProperty("name").GetString() == "greet");

        Assert.NotEqual(default, greetTool);

        var nameParam = greetTool
            .GetProperty("inputSchema")
            .GetProperty("properties")
            .GetProperty("name");

        Assert.Equal("The name to greet", nameParam.GetProperty("description").GetString());

        // Framework parameters must not appear
        var properties = greetTool.GetProperty("inputSchema").GetProperty("properties");
        Assert.False(properties.TryGetProperty("logger", out _));
        Assert.False(properties.TryGetProperty("cancellationToken", out _));
    }

    // ── Helper ─────────────────────────────────────────────────────────────

    private async Task<HttpResponseMessage> PostMcpRequestAsync(object body)
    {
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _client!.PostAsync("/mcp", content);
    }
}

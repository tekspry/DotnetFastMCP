using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FastMCP.Attributes;
using FastMCP.Health;
using FastMCP.Hosting;
using FastMCP.Protocol;
using FastMCP.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FastMCP.IntegrationTests.Http;

public static class ServerTools
{
    [McpTool("calculate_sum", Description = "Calculates the sum of two integers")]
    public static int CalculateSum(int a, int b) => a + b;

    [McpResource("resource://status")]
    public static object GetStatus() => new { state = "active" };

    [McpPrompt("review_code", Description = "Code review prompt")]
    public static string ReviewCode(string snippet) => $"Please review:\n{snippet}";
}

public class McpServerHttpIntegrationTests : IAsyncLifetime
{
    private WebApplication? _app;
    private HttpClient? _client;
    private string _serverAddress = "";

    public async Task InitializeAsync()
    {
        var server = new FastMCPServer("IntegrationServer");
        var builder = McpServerBuilder.Create(server, new[] { "--urls", "http://127.0.0.1:0" });
        builder.WithComponentsFrom(typeof(ServerTools).Assembly);
        builder.WithHealthChecks(opt => opt.AddCheck("custom_check", () => true));

        _app = builder.Build();
        await _app.StartAsync();

        var serverFeature = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        _serverAddress = serverFeature?.Addresses.First() ?? "http://127.0.0.1:5000";
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

    [Fact]
    public async Task RootEndpoint_ReturnsServerInfo()
    {
        var response = await _client!.GetAsync("/");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("IntegrationServer", content);
        Assert.Contains("Registered Tools", content);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        var response = await _client!.GetAsync("/mcp/health");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":\"Healthy\"", json);
    }

    [Fact]
    public async Task McpEndpoint_ToolsCall_ExecutesSuccessfully()
    {
        var request = new
        {
            jsonrpc = "2.0",
            method = "calculate_sum",
            @params = new { a = 12, b = 8 },
            id = 1
        };

        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await _client!.PostAsync("/mcp", content);
        response.EnsureSuccessStatusCode();

        var jsonDoc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var textResult = jsonDoc.RootElement
            .GetProperty("result")
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();
        Assert.Equal("20", textResult);
    }

    [Fact]
    public async Task McpEndpoint_UnknownMethod_ReturnsMethodNotFound()
    {
        var request = new
        {
            jsonrpc = "2.0",
            method = "non_existent_tool",
            @params = new { },
            id = 2
        };

        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await _client!.PostAsync("/mcp", content);
        response.EnsureSuccessStatusCode();

        var jsonDoc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errorCode = jsonDoc.RootElement.GetProperty("error").GetProperty("code").GetInt32();
        Assert.Equal(-32601, errorCode);
    }
}

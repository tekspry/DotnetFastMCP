using FastMCP.Attributes;
using FastMCP.Client;
using FastMCP.Client.Transports;
using FastMCP.Hosting;
using FastMCP.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FastMCP.IntegrationTests.Sse;

public static class SseTestTools
{
    [McpTool("multiply", Description = "Multiplies two numbers")]
    public static int Multiply(int a, int b) => a * b;
}

public class McpServerSseIntegrationTests : IAsyncLifetime
{
    private WebApplication? _app;
    private string _serverAddress = "";

    public async Task InitializeAsync()
    {
        var server = new FastMCPServer("SseIntegrationServer");
        var builder = McpServerBuilder.Create(server, new[] { "--urls", "http://127.0.0.1:0" });
        builder.WithComponentsFrom(typeof(SseTestTools).Assembly);

        _app = builder.Build();
        await _app.StartAsync();

        var serverFeature = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
        _serverAddress = serverFeature?.Addresses.First() ?? "http://127.0.0.1:5000";
    }

    public async Task DisposeAsync()
    {
        if (_app != null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    [Fact]
    public async Task SseTransport_ConnectAndCallTool_Succeeds()
    {
        var sseUrl = $"{_serverAddress}/sse";
        var transport = new SseClientTransport(sseUrl);
        await using var client = new McpClient(transport);

        await client.ConnectAsync();
        // Give connection and endpoint discovery a brief moment
        await Task.Delay(200);

        var tools = await client.ListToolsAsync();
        Assert.NotNull(tools);
        Assert.Contains(tools.Tools, t => t.Name == "multiply");
    }
}

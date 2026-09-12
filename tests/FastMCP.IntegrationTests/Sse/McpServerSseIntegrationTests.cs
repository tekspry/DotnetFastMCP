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

public record TestPerson(string Name, int Age);

public static class SseTestTools
{
    [McpTool("multiply", Description = "Multiplies two numbers")]
    public static int Multiply(int a, int b) => a * b;

    [McpTool("sse_echo", Description = "Echoes a message")]
    public static string Echo(string message) => $"Echo: {message}";

    [McpTool("sse_get_person", Description = "Returns a person model")]
    public static TestPerson GetPerson(string name, int age) => new(name, age);
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

        // 1. Primitive int tool call
        var product = await client.CallToolAsync<int>("multiply", new { a = 6, b = 7 });
        Assert.Equal(42, product);

        // 2. String tool call
        var greeting = await client.CallToolAsync<string>("sse_echo", new { message = "Hello" });
        Assert.Equal("Echo: Hello", greeting);

        // 3. POCO complex model tool call
        var person = await client.CallToolAsync<TestPerson>("sse_get_person", new { name = "Bob", age = 30 });
        Assert.NotNull(person);
        Assert.Equal("Bob", person.Name);
        Assert.Equal(30, person.Age);

        // 4. Raw CallToolResult envelope call
        var rawResult = await client.CallToolAsync("multiply", new { a = 2, b = 3 });
        Assert.NotNull(rawResult);
        Assert.False(rawResult.IsError);
        Assert.Equal("6", ((FastMCP.Protocol.TextContent)rawResult.Content[0]).Text);
    }
}

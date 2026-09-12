using FastMCP.Client;
using FastMCP.Client.Transports;
using FastMCP.Protocol;
using System.Text.Json;
using System.Threading.Channels;
using Xunit;

namespace FastMCP.Tests.Client;

public record WeatherReport(string City, int Temperature, bool IsSunny);

public class TestClientTransport : IClientTransport
{
    private readonly Channel<string> _incomingMessages = Channel.CreateUnbounded<string>();
    public bool IsConnected { get; private set; } = true;
    public Func<JsonRpcRequest, string?>? ResponseGenerator { get; set; }

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        IsConnected = true;
        return Task.CompletedTask;
    }

    public Task SendAsync(object message, CancellationToken cancellationToken = default)
    {
        if (message is JsonRpcRequest req && ResponseGenerator != null)
        {
            var responseJson = ResponseGenerator(req);
            if (responseJson != null)
            {
                _incomingMessages.Writer.TryWrite(responseJson);
            }
        }
        return Task.CompletedTask;
    }

    public async Task<string?> ReadNextMessageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _incomingMessages.Reader.ReadAsync(cancellationToken);
        }
        catch (ChannelClosedException)
        {
            return null;
        }
    }

    public ValueTask DisposeAsync()
    {
        IsConnected = false;
        _incomingMessages.Writer.TryComplete();
        return ValueTask.CompletedTask;
    }
}

public class McpClientToolCallTests
{
    [Fact]
    public async Task CallToolAsync_IntPrimitive_Succeeds()
    {
        var transport = new TestClientTransport();
        transport.ResponseGenerator = req =>
            $"{{\"jsonrpc\":\"2.0\",\"id\":{req.Id},\"result\":{{\"content\":[{{\"type\":\"text\",\"text\":\"65\"}}],\"isError\":false}}}}";

        await using var client = new McpClient(transport);
        await client.ConnectAsync();

        var result = await client.CallToolAsync<int>("Add", new { a = 10, b = 55 });

        Assert.Equal(65, result);
    }

    [Fact]
    public async Task CallToolAsync_String_Succeeds()
    {
        var transport = new TestClientTransport();
        transport.ResponseGenerator = req =>
            $"{{\"jsonrpc\":\"2.0\",\"id\":{req.Id},\"result\":{{\"content\":[{{\"type\":\"text\",\"text\":\"Hello World\"}}],\"isError\":false}}}}";

        await using var client = new McpClient(transport);
        await client.ConnectAsync();

        var result = await client.CallToolAsync<string>("Echo", new { message = "Hello World" });

        Assert.Equal("Hello World", result);
    }

    [Fact]
    public async Task CallToolAsync_BoolPrimitive_Succeeds()
    {
        var transport = new TestClientTransport();
        transport.ResponseGenerator = req =>
            $"{{\"jsonrpc\":\"2.0\",\"id\":{req.Id},\"result\":{{\"content\":[{{\"type\":\"text\",\"text\":\"true\"}}],\"isError\":false}}}}";

        await using var client = new McpClient(transport);
        await client.ConnectAsync();

        var result = await client.CallToolAsync<bool>("IsValid", new { });

        Assert.True(result);
    }

    [Fact]
    public async Task CallToolAsync_DoublePrimitive_Succeeds()
    {
        var transport = new TestClientTransport();
        transport.ResponseGenerator = req =>
            $"{{\"jsonrpc\":\"2.0\",\"id\":{req.Id},\"result\":{{\"content\":[{{\"type\":\"text\",\"text\":\"3.14159\"}}],\"isError\":false}}}}";

        await using var client = new McpClient(transport);
        await client.ConnectAsync();

        var result = await client.CallToolAsync<double>("GetPi", new { });

        Assert.Equal(3.14159, result);
    }

    [Fact]
    public async Task CallToolAsync_PocoComplexType_Succeeds()
    {
        var transport = new TestClientTransport();
        var weatherJson = JsonSerializer.Serialize(new { city = "Seattle", temperature = 72, isSunny = true });
        // Escape quotes for json string
        var escapedWeatherJson = weatherJson.Replace("\"", "\\\"");

        transport.ResponseGenerator = req =>
            $"{{\"jsonrpc\":\"2.0\",\"id\":{req.Id},\"result\":{{\"content\":[{{\"type\":\"text\",\"text\":\"{escapedWeatherJson}\"}}],\"isError\":false}}}}";

        await using var client = new McpClient(transport);
        await client.ConnectAsync();

        var report = await client.CallToolAsync<WeatherReport>("get_weather", new { city = "Seattle" });

        Assert.NotNull(report);
        Assert.Equal("Seattle", report.City);
        Assert.Equal(72, report.Temperature);
        Assert.True(report.IsSunny);
    }

    [Fact]
    public async Task CallToolAsync_RawCallToolResult_Succeeds()
    {
        var transport = new TestClientTransport();
        transport.ResponseGenerator = req =>
            $"{{\"jsonrpc\":\"2.0\",\"id\":{req.Id},\"result\":{{\"content\":[{{\"type\":\"text\",\"text\":\"raw_value\"}}],\"isError\":false}}}}";

        await using var client = new McpClient(transport);
        await client.ConnectAsync();

        var result = await client.CallToolAsync("TestRaw", new { });

        Assert.NotNull(result);
        Assert.False(result.IsError);
        Assert.Single(result.Content);
        Assert.Equal("raw_value", ((TextContent)result.Content[0]).Text);
    }

    [Fact]
    public async Task CallToolAsync_ToolError_ThrowsInvalidOperationException()
    {
        var transport = new TestClientTransport();
        transport.ResponseGenerator = req =>
            $"{{\"jsonrpc\":\"2.0\",\"id\":{req.Id},\"result\":{{\"content\":[{{\"type\":\"text\",\"text\":\"Calculation overflow\"}}],\"isError\":true}}}}";

        await using var client = new McpClient(transport);
        await client.ConnectAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.CallToolAsync<int>("Fail", new { }));

        Assert.Contains("Calculation overflow", ex.Message);
    }
}

using System.IO;
using System.Text;
using System.Text.Json;
using FastMCP.Hosting;
using FastMCP.Protocol;
using FastMCP.Server;
using FastMCP.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FastMCP.Tests.Protocol;

public class McpProtocolMiddlewareTests
{
    private readonly McpRequestHandler _handler;
    private readonly FastMCPServer _server;

    public McpProtocolMiddlewareTests()
    {
        var services = new ServiceCollection();
        services.AddAuthorizationCore();
        services.AddSingleton<IMcpStorage, InMemoryMcpStorage>();
        var sp = services.BuildServiceProvider();

        var mockAuthService = new Mock<IAuthorizationService>();
        _server = new FastMCPServer("test-server");
        _handler = new McpRequestHandler(mockAuthService.Object, new InMemoryMcpStorage(), sp);
    }

    [Fact]
    public async Task InvokeAsync_NonMcpPath_CallsNextMiddleware()
    {
        bool nextCalled = false;
        var middleware = new McpProtocolMiddleware(ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/other";
        context.Request.Method = "POST";

        await middleware.InvokeAsync(context, _server, _handler, NullLogger<McpProtocolMiddleware>.Instance);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_GetMethodOnMcpPath_CallsNextMiddleware()
    {
        bool nextCalled = false;
        var middleware = new McpProtocolMiddleware(ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        context.Request.Path = "/mcp";
        context.Request.Method = "GET";

        await middleware.InvokeAsync(context, _server, _handler, NullLogger<McpProtocolMiddleware>.Instance);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_MalformedJson_ReturnsParseError()
    {
        var middleware = new McpProtocolMiddleware(ctx => Task.CompletedTask);

        var context = new DefaultHttpContext();
        context.Request.Path = "/mcp";
        context.Request.Method = "POST";
        var malformedJson = "{ \"jsonrpc\": \"2.0\", \"method\": "; // incomplete
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(malformedJson));
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context, _server, _handler, NullLogger<McpProtocolMiddleware>.Instance);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseJson = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var jsonDoc = JsonDocument.Parse(responseJson);

        Assert.Equal(-32700, jsonDoc.RootElement.GetProperty("error").GetProperty("code").GetInt32());
    }

    [Fact]
    public async Task InvokeAsync_InvalidJsonRpcVersion_ReturnsInvalidRequest()
    {
        var middleware = new McpProtocolMiddleware(ctx => Task.CompletedTask);

        var context = new DefaultHttpContext();
        context.Request.Path = "/mcp";
        context.Request.Method = "POST";
        var invalidJson = "{ \"jsonrpc\": \"1.0\", \"method\": \"ping\" }";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(invalidJson));
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context, _server, _handler, NullLogger<McpProtocolMiddleware>.Instance);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseJson = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var jsonDoc = JsonDocument.Parse(responseJson);

        Assert.Equal(-32600, jsonDoc.RootElement.GetProperty("error").GetProperty("code").GetInt32());
    }
}

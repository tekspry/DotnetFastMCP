using System.Security.Claims;
using System.Text.Json;
using FastMCP.Hosting;
using FastMCP.Protocol;
using FastMCP.Server;
using FastMCP.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace FastMCP.Tests.Protocol;

public class TestTools
{
    public static int Add(int a, int b) => a + b;
    public static string Echo(string message, string prefix = "Echo:") => $"{prefix} {message}";
    public static object EchoComplex(ComplexInput input) => new { Received = input.Data, Count = input.Items?.Count ?? 0 };
    public static string ThrowingTool() => throw new InvalidOperationException("tool_failure_reason");
    public static CallToolResult CustomResultTool() => new CallToolResult 
    { 
        Content = new List<ContentItem> { new TextContent { Text = "explicit_result" } } 
    };
    public static object GetConfig() => new { Version = "1.0.0" };
}

public class ComplexInput
{
    public string Data { get; set; } = "";
    public List<int>? Items { get; set; }
}

public class McpRequestHandlerTests
{
    private readonly McpRequestHandler _handler;
    private readonly FastMCPServer _server;

    public McpRequestHandlerTests()
    {
        var services = new ServiceCollection();
        services.AddAuthorizationCore();
        services.AddSingleton<IMcpStorage, InMemoryMcpStorage>();
        var sp = services.BuildServiceProvider();

        var mockAuthService = new Mock<IAuthorizationService>();
        mockAuthService
            .Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());

        _server = new FastMCPServer("test-server");
        _server.Tools["add"] = typeof(TestTools).GetMethod(nameof(TestTools.Add))!;
        _server.Tools["echo"] = typeof(TestTools).GetMethod(nameof(TestTools.Echo))!;
        _server.Tools["echo_complex"] = typeof(TestTools).GetMethod(nameof(TestTools.EchoComplex))!;
        _server.Tools["throw_err"] = typeof(TestTools).GetMethod(nameof(TestTools.ThrowingTool))!;
        _server.Tools["custom_res"] = typeof(TestTools).GetMethod(nameof(TestTools.CustomResultTool))!;
        _server.Resources["resource://config"] = typeof(TestTools).GetMethod(nameof(TestTools.GetConfig))!;

        _handler = new McpRequestHandler(mockAuthService.Object, new InMemoryMcpStorage(), sp);
    }

    [Fact]
    public async Task HandlePing_ReturnsPong()
    {
        var request = new JsonRpcRequest { JsonRpc = "2.0", Id = 1, Method = "ping" };
        var response = await _handler.HandleRequestAsync(request, _server, null);

        Assert.NotNull(response);
        Assert.Equal(1, response.Id);
        Assert.Equal("pong", response.Result);
        Assert.Null(response.Error);
    }

    [Fact]
    public async Task HandleInitialize_ReturnsServerInfo()
    {
        var request = new JsonRpcRequest { JsonRpc = "2.0", Id = 1, Method = "initialize" };
        var response = await _handler.HandleRequestAsync(request, _server, null);

        Assert.NotNull(response);
        Assert.Null(response.Error);
        Assert.NotNull(response.Result);
    }

    [Fact]
    public async Task ToolExecution_PositionalParameters_Succeeds()
    {
        var paramsElement = JsonSerializer.Deserialize<JsonElement>("[5, 10]");
        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = 2,
            Method = "add",
            Params = paramsElement
        };

        var response = await _handler.HandleRequestAsync(request, _server, null);

        Assert.NotNull(response);
        Assert.Null(response.Error);
        var result = Assert.IsType<CallToolResult>(response.Result);
        var textContent = Assert.IsType<TextContent>(result.Content[0]);
        Assert.Equal("15", textContent.Text);
    }

    [Fact]
    public async Task ToolExecution_NamedParameters_Succeeds()
    {
        var paramsElement = JsonSerializer.Deserialize<JsonElement>("{\"a\": 20, \"b\": 30}");
        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = 3,
            Method = "add",
            Params = paramsElement
        };

        var response = await _handler.HandleRequestAsync(request, _server, null);

        Assert.NotNull(response);
        Assert.Null(response.Error);
        var result = Assert.IsType<CallToolResult>(response.Result);
        var textContent = Assert.IsType<TextContent>(result.Content[0]);
        Assert.Equal("50", textContent.Text);
    }

    [Fact]
    public async Task ToolExecution_DefaultOptionalParameters_Succeeds()
    {
        var paramsElement = JsonSerializer.Deserialize<JsonElement>("{\"message\": \"Hello World\"}");
        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = 4,
            Method = "echo",
            Params = paramsElement
        };

        var response = await _handler.HandleRequestAsync(request, _server, null);

        Assert.NotNull(response);
        Assert.Null(response.Error);
        var result = Assert.IsType<CallToolResult>(response.Result);
        var textContent = Assert.IsType<TextContent>(result.Content[0]);
        Assert.Equal("Echo: Hello World", textContent.Text);
    }

    [Fact]
    public async Task ToolExecution_ComplexObjectBinding_Succeeds()
    {
        var paramsElement = JsonSerializer.Deserialize<JsonElement>("{\"input\": {\"data\": \"test_val\", \"items\": [1, 2, 3]}}");
        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = 5,
            Method = "echo_complex",
            Params = paramsElement
        };

        var response = await _handler.HandleRequestAsync(request, _server, null);

        Assert.NotNull(response);
        Assert.Null(response.Error);
    }

    [Fact]
    public async Task NonExistentMethod_ReturnsMethodNotFound()
    {
        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = 6,
            Method = "NonExistentMethod",
            Params = JsonSerializer.Deserialize<JsonElement>("{}")
        };

        var response = await _handler.HandleRequestAsync(request, _server, null);

        Assert.NotNull(response);
        Assert.NotNull(response.Error);
        Assert.Equal(-32601, response.Error!.Code);
    }

    [Fact]
    public async Task InvalidParams_ReturnsInvalidParamsError()
    {
        var paramsElement = JsonSerializer.Deserialize<JsonElement>("[\"not_a_number\", \"also_not_a_number\"]");
        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = 7,
            Method = "add",
            Params = paramsElement
        };

        var response = await _handler.HandleRequestAsync(request, _server, null);

        Assert.NotNull(response);
        Assert.NotNull(response.Error);
        Assert.Equal(-32602, response.Error!.Code);
    }

    [Fact]
    public async Task HandleToolsList_ReturnsAllRegisteredTools()
    {
        var request = new JsonRpcRequest { JsonRpc = "2.0", Id = 8, Method = "tools/list" };
        var response = await _handler.HandleRequestAsync(request, _server, null);

        Assert.NotNull(response);
        Assert.Null(response.Error);
        var result = Assert.IsType<ListToolsResult>(response.Result);
        Assert.Contains(result.Tools, t => t.Name == "add");
        Assert.Contains(result.Tools, t => t.Name == "echo");
    }

    [Fact]
    public async Task HandleResourcesList_ReturnsAllRegisteredResources()
    {
        var request = new JsonRpcRequest { JsonRpc = "2.0", Id = 9, Method = "resources/list" };
        var response = await _handler.HandleRequestAsync(request, _server, null);

        Assert.NotNull(response);
        Assert.Null(response.Error);
        var result = Assert.IsType<ListResourcesResult>(response.Result);
        Assert.Contains(result.Resources, r => r.Name == "resource://config");
    }

    [Fact]
    public async Task HandleResourcesRead_NonExistentResource_ReturnsMethodNotFound()
    {
        var paramsElement = JsonSerializer.Deserialize<JsonElement>("{\"uri\": \"resource://missing\"}");
        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = 10,
            Method = "resources/read",
            Params = paramsElement
        };

        var response = await _handler.HandleRequestAsync(request, _server, null);

        Assert.NotNull(response);
        Assert.NotNull(response.Error);
        Assert.Equal(-32601, response.Error!.Code);
    }

    [Fact]
    public async Task ToolExecution_ThrowingTool_ReturnsInternalError()
    {
        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = 11,
            Method = "throw_err",
            Params = JsonSerializer.Deserialize<JsonElement>("{}")
        };

        var response = await _handler.HandleRequestAsync(request, _server, null);

        Assert.NotNull(response);
        Assert.NotNull(response.Error);
        Assert.Equal(-32603, response.Error!.Code);
        Assert.Contains("tool_failure_reason", response.Error!.Message);
    }

    [Fact]
    public async Task ToolExecution_DirectCallToolResult_ReturnsExactResult()
    {
        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Id = 12,
            Method = "custom_res",
            Params = JsonSerializer.Deserialize<JsonElement>("{}")
        };

        var response = await _handler.HandleRequestAsync(request, _server, null);

        Assert.NotNull(response);
        Assert.Null(response.Error);
        var result = Assert.IsType<CallToolResult>(response.Result);
        var textContent = Assert.IsType<TextContent>(result.Content[0]);
        Assert.Equal("explicit_result", textContent.Text);
    }
}

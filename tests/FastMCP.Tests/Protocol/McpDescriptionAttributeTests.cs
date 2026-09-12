using System.Security.Claims;
using System.Text.Json;
using FastMCP.Attributes;
using FastMCP.Hosting;
using FastMCP.Protocol;
using FastMCP.Server;
using FastMCP.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace FastMCP.Tests.Protocol;

// ── Test fixtures ────────────────────────────────────────────────────────────

public static class SchemaTestTools
{
    // Tool with [McpDescription] on all parameters
    [McpTool("described_tool", Description = "A tool with described parameters")]
    public static string DescribedTool(
        [McpDescription("The user's name")] string name,
        [McpDescription("The user's age in years")] int age,
        [McpDescription("Whether to send a welcome email")] bool sendEmail = false)
        => $"{name},{age},{sendEmail}";

    // Tool without any [McpDescription] — graceful fallback
    [McpTool("plain_tool")]
    public static string PlainTool(string value) => value;

    // Tool with framework-injected parameters that must be hidden from the schema
    [McpTool("injected_params_tool")]
    public static async Task<string> InjectedParamsTool(
        [McpDescription("The real user-visible parameter")] string realParam,
        ClaimsPrincipal user,
        McpContext context,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return realParam;
    }

    // Tool with ALL injected types and no real parameters
    [McpTool("all_injected_tool")]
    public static async Task<string> AllInjectedTool(
        ClaimsPrincipal user,
        McpContext context,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return "ok";
    }
}

// ── Tests ────────────────────────────────────────────────────────────────────

public class McpDescriptionAttributeTests
{
    private readonly McpRequestHandler _handler;
    private readonly FastMCPServer _server;

    public McpDescriptionAttributeTests()
    {
        var services = new ServiceCollection();
        services.AddAuthorizationCore();
        services.AddSingleton<IMcpStorage, InMemoryMcpStorage>();
        var sp = services.BuildServiceProvider();

        var mockAuth = new Mock<IAuthorizationService>();
        mockAuth
            .Setup(a => a.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());

        _server = new FastMCPServer("schema-test");
        _server.Tools["described_tool"] = typeof(SchemaTestTools).GetMethod(nameof(SchemaTestTools.DescribedTool))!;
        _server.Tools["plain_tool"] = typeof(SchemaTestTools).GetMethod(nameof(SchemaTestTools.PlainTool))!;
        _server.Tools["injected_params_tool"] = typeof(SchemaTestTools).GetMethod(nameof(SchemaTestTools.InjectedParamsTool))!;
        _server.Tools["all_injected_tool"] = typeof(SchemaTestTools).GetMethod(nameof(SchemaTestTools.AllInjectedTool))!;

        _handler = new McpRequestHandler(mockAuth.Object, new InMemoryMcpStorage(), sp);
    }

    // ── [McpDescription] tests ─────────────────────────────────────────────

    [Fact]
    public async Task ToolsList_WithMcpDescription_PopulatesParameterDescription()
    {
        var request = new JsonRpcRequest { JsonRpc = "2.0", Id = 1, Method = "tools/list" };
        var response = await _handler.HandleRequestAsync(request, _server, null);

        Assert.Null(response.Error);
        var result = Assert.IsType<ListToolsResult>(response.Result);
        var tool = result.Tools.First(t => t.Name == "described_tool");

        // Each described parameter must have a non-empty description in the schema
        var nameSchema = GetPropertySchema(tool.InputSchema, "name");
        var ageSchema = GetPropertySchema(tool.InputSchema, "age");
        var emailSchema = GetPropertySchema(tool.InputSchema, "sendEmail");

        Assert.Equal("The user's name", GetDescription(nameSchema));
        Assert.Equal("The user's age in years", GetDescription(ageSchema));
        Assert.Equal("Whether to send a welcome email", GetDescription(emailSchema));
    }

    [Fact]
    public async Task ToolsList_WithMcpDescription_TypesAreCorrect()
    {
        var request = new JsonRpcRequest { JsonRpc = "2.0", Id = 1, Method = "tools/list" };
        var response = await _handler.HandleRequestAsync(request, _server, null);

        var result = Assert.IsType<ListToolsResult>(response.Result);
        var tool = result.Tools.First(t => t.Name == "described_tool");

        Assert.Equal("string", GetType(GetPropertySchema(tool.InputSchema, "name")));
        Assert.Equal("integer", GetType(GetPropertySchema(tool.InputSchema, "age")));
        Assert.Equal("boolean", GetType(GetPropertySchema(tool.InputSchema, "sendEmail")));
    }

    [Fact]
    public async Task ToolsList_WithoutMcpDescription_DescriptionIsEmpty()
    {
        var request = new JsonRpcRequest { JsonRpc = "2.0", Id = 1, Method = "tools/list" };
        var response = await _handler.HandleRequestAsync(request, _server, null);

        var result = Assert.IsType<ListToolsResult>(response.Result);
        var tool = result.Tools.First(t => t.Name == "plain_tool");

        var valueSchema = GetPropertySchema(tool.InputSchema, "value");
        Assert.Equal(string.Empty, GetDescription(valueSchema));
    }

    // ── Framework-injected parameter exclusion tests ───────────────────────

    [Fact]
    public async Task ToolsList_ClaimsPrincipalParam_IsExcludedFromSchema()
    {
        var request = new JsonRpcRequest { JsonRpc = "2.0", Id = 1, Method = "tools/list" };
        var response = await _handler.HandleRequestAsync(request, _server, null);

        var result = Assert.IsType<ListToolsResult>(response.Result);
        var tool = result.Tools.First(t => t.Name == "injected_params_tool");

        Assert.False(tool.InputSchema.Properties.ContainsKey("user"),
            "ClaimsPrincipal parameter 'user' must not appear in schema");
    }

    [Fact]
    public async Task ToolsList_McpContextParam_IsExcludedFromSchema()
    {
        var request = new JsonRpcRequest { JsonRpc = "2.0", Id = 1, Method = "tools/list" };
        var response = await _handler.HandleRequestAsync(request, _server, null);

        var result = Assert.IsType<ListToolsResult>(response.Result);
        var tool = result.Tools.First(t => t.Name == "injected_params_tool");

        Assert.False(tool.InputSchema.Properties.ContainsKey("context"),
            "McpContext parameter 'context' must not appear in schema");
    }

    [Fact]
    public async Task ToolsList_CancellationTokenParam_IsExcludedFromSchema()
    {
        var request = new JsonRpcRequest { JsonRpc = "2.0", Id = 1, Method = "tools/list" };
        var response = await _handler.HandleRequestAsync(request, _server, null);

        var result = Assert.IsType<ListToolsResult>(response.Result);
        var tool = result.Tools.First(t => t.Name == "injected_params_tool");

        Assert.False(tool.InputSchema.Properties.ContainsKey("cancellationToken"),
            "CancellationToken parameter must not appear in schema");
    }

    [Fact]
    public async Task ToolsList_OnlyRealUserParamAppears_WhenMixedWithInjected()
    {
        var request = new JsonRpcRequest { JsonRpc = "2.0", Id = 1, Method = "tools/list" };
        var response = await _handler.HandleRequestAsync(request, _server, null);

        var result = Assert.IsType<ListToolsResult>(response.Result);
        var tool = result.Tools.First(t => t.Name == "injected_params_tool");

        // Only the real parameter should appear
        Assert.Single(tool.InputSchema.Properties);
        Assert.True(tool.InputSchema.Properties.ContainsKey("realParam"));
        Assert.Equal("The real user-visible parameter",
            GetDescription(GetPropertySchema(tool.InputSchema, "realParam")));
    }

    [Fact]
    public async Task ToolsList_AllInjectedParams_SchemaHasNoProperties()
    {
        var request = new JsonRpcRequest { JsonRpc = "2.0", Id = 1, Method = "tools/list" };
        var response = await _handler.HandleRequestAsync(request, _server, null);

        var result = Assert.IsType<ListToolsResult>(response.Result);
        var tool = result.Tools.First(t => t.Name == "all_injected_tool");

        Assert.Empty(tool.InputSchema.Properties);
        Assert.Empty(tool.InputSchema.Required);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static JsonElement GetPropertySchema(InputSchema schema, string paramName)
    {
        Assert.True(schema.Properties.TryGetValue(paramName, out var prop),
            $"Property '{paramName}' not found in schema. Available: {string.Join(", ", schema.Properties.Keys)}");
        var json = System.Text.Json.JsonSerializer.Serialize(prop);
        return System.Text.Json.JsonSerializer.Deserialize<JsonElement>(json);
    }

    private static string GetDescription(JsonElement propSchema)
        => propSchema.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";

    private static string GetType(JsonElement propSchema)
        => propSchema.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";
}

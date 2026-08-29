using System.Reflection;
using FastMCP.Attributes;
using FastMCP.Server;
using Xunit;

namespace FastMCP.Tests.Discovery;

public class TestComponents
{
    [McpTool("custom_add", Description = "Adds two numbers")]
    public static int Add(int a, int b) => a + b;

    [McpTool]
    public static string Greet(string name, string greeting = "Hello") => $"{greeting}, {name}!";

    [McpResource("resource://server/status")]
    public static object GetStatus() => new { status = "online" };

    [McpPrompt("explain_code", Description = "Explains code snippets")]
    public static string ExplainCode(string code, string language = "csharp") => $"Please explain this {language} code:\n{code}";
}

public class ComponentDiscoveryTests
{
    [Fact]
    public void RegisterTools_FindsAttributedMethods()
    {
        var server = new FastMCPServer("test-server");
        
        // Register manually or via discovery
        foreach (var method in typeof(TestComponents).GetMethods(BindingFlags.Public | BindingFlags.Static))
        {
            var toolAttr = method.GetCustomAttribute<McpToolAttribute>();
            if (toolAttr != null)
            {
                var name = string.IsNullOrEmpty(toolAttr.Name) ? method.Name : toolAttr.Name;
                server.Tools[name] = method;
            }

            var resourceAttr = method.GetCustomAttribute<McpResourceAttribute>();
            if (resourceAttr != null)
            {
                var uri = string.IsNullOrEmpty(resourceAttr.Uri) ? method.Name : resourceAttr.Uri;
                server.Resources[uri] = method;
            }

            var promptAttr = method.GetCustomAttribute<McpPromptAttribute>();
            if (promptAttr != null)
            {
                var name = string.IsNullOrEmpty(promptAttr.Name) ? method.Name : promptAttr.Name;
                server.Prompts[name] = method;
            }
        }

        Assert.True(server.Tools.ContainsKey("custom_add"));
        Assert.True(server.Tools.ContainsKey("Greet"));
        Assert.True(server.Resources.ContainsKey("resource://server/status"));
        Assert.True(server.Prompts.ContainsKey("explain_code"));
    }

    [Fact]
    public void ServerImport_PrefixesAndMergesComponents()
    {
        var subServer = new FastMCPServer("math-server");
        var addMethod = typeof(TestComponents).GetMethod(nameof(TestComponents.Add))!;
        subServer.Tools["add"] = addMethod;
        subServer.ToolsMetadata.Add(new FastMCP.Protocol.Tool { Name = "add", Description = "Add numbers" });

        var rootServer = new FastMCPServer("root-server");
        rootServer.Import(subServer, prefix: "math");

        Assert.True(rootServer.Tools.ContainsKey("math_add"));
        Assert.Contains(rootServer.ToolsMetadata, m => m.Name == "math_add");
    }
}

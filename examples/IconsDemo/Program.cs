using FastMCP.Attributes;
using FastMCP.Hosting;
using FastMCP.Server;
using FastMCP.Protocol; // For Prompt result types
using FastMCP;
using System.Reflection;

// 1. Create Server with Icon
var server = new FastMCPServer("IconsDemo");
server.Icon = "https://example.com/server-icon.png"; // Set Server Icon

var builder = McpServerBuilder.Create(server, args);

// 2. Register Tools
builder.WithComponentsFrom(Assembly.GetExecutingAssembly());

// 3. Build & Run
var app = builder.Build();
await app.RunMcpAsync(args);

public static class IconTools
{
    [McpTool(Icon = "https://example.com/tool-icon.png")]
    public static string ToolWithIcon()
    {
        return "I have an icon!";
    }

    [McpResource("icon-resource", Icon = "https://example.com/resource-icon.png")]
    public static string ResourceWithIcon()
    {
        return "I am a resource with an icon.";
    }

    [McpPrompt("icon-prompt", Icon = "https://example.com/prompt-icon.png")]
    public static GetPromptResult PromptWithIcon()
    {
        return new GetPromptResult
        {
            Description = "A prompt with an icon",
            Messages = new List<PromptMessage>
            {
                new PromptMessage { Role = "user", Content = new { type = "text", text = "Hello" } }
            }
        };
    }
}

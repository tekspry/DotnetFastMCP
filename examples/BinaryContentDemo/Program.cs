using FastMCP.Attributes;
using FastMCP.Hosting;
using FastMCP.Server;
using FastMCP.Protocol;
using FastMCP;
using System.Reflection;

var server = new FastMCPServer("BinaryContentDemo");
var builder = McpServerBuilder.Create(server, args);

builder.WithComponentsFrom(Assembly.GetExecutingAssembly());

var app = builder.Build();
await app.RunMcpAsync(args);

public static class BinaryTools
{
    [McpTool("get_screenshot")]
    public static CallToolResult GetScreenshot()
    {
        // Simulate a screenshot (1x1 red pixel)
        var redPixel = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKwM+AAAAABJRU5ErkJggg==";
        
        return new CallToolResult 
        {
            Content = new List<ContentItem>
            {
                new TextContent { Text = "Here is your screenshot:" },
                new ImageContent { Data = redPixel, MimeType = "image/png" }
            }
        };
    }

    [McpPrompt("explain_image")]
    public static GetPromptResult ExplainImage()
    {
        return new GetPromptResult
        {
            Description = "A prompt with an image context",
            Messages = new List<PromptMessage>
            {
                new PromptMessage 
                { 
                    Role = "user", 
                    Content = new ImageContent 
                    { 
                        Data = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKwM+AAAAABJRU5ErkJggg==", 
                        MimeType = "image/png" 
                    } 
                },
                 new PromptMessage 
                { 
                    Role = "user", 
                    Content = new TextContent { Text = "What is this image?" } 
                }
            }
        };
    }
}

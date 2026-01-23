using System.Text.Json.Serialization;

namespace FastMCP.Protocol;

public class PromptMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";
    [JsonPropertyName("content")]
    public ContentItem  Content { get; set; } = new TextContent(); // Initialize with default
}
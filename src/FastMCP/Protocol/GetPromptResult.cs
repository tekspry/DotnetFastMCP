using System.Text.Json.Serialization;

namespace FastMCP.Protocol;

public class GetPromptResult
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    [JsonPropertyName("messages")]
    public List<PromptMessage> Messages { get; set; } = new();
}
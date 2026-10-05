using System.Text.Json.Serialization;

namespace FastMCP.Protocol;

public class GetPromptResult
{
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    [JsonPropertyName("messages")]
    public List<PromptMessage> Messages { get; set; } = new();
}
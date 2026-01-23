using System.Text.Json.Serialization;

namespace FastMCP.Protocol;

/// <summary>
/// Represents a prompt definition returned by prompts/list.
/// </summary>
public class Prompt
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    [JsonPropertyName("arguments")]
    public List<PromptArgument>? Arguments { get; set; }

     [JsonPropertyName("icon")]
    public string? Icon { get; set; }
}
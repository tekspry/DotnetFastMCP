using System.Text.Json.Serialization;
namespace FastMCP.Protocol;

public class PromptArgument
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    [JsonPropertyName("required")]
    public bool Required { get; set; }
}
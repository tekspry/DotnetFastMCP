using System.Text.Json;

namespace FastMCP.Protocol;

/// <summary>
/// Shared JSON serializer options for MCP protocol messaging.
/// </summary>
/// <remarks>
/// Null-omission is intentionally NOT applied globally, so user-defined tool/resource/prompt
/// payloads keep their null properties exactly as before. Optional MCP protocol fields
/// (e.g. Prompt.description, Resource.mimeType) are omitted when null via explicit
/// [JsonIgnore(Condition = WhenWritingNull)] attributes on the protocol types, because the
/// MCP TypeScript SDK (Zod) rejects null for optional string fields.
/// </remarks>
public static class McpJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}

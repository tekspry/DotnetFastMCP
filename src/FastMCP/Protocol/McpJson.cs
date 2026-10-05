using System.Text.Json;
using System.Text.Json.Serialization;

namespace FastMCP.Protocol;

/// <summary>
/// Provides shared, standardized JSON serializer options for all MCP protocol messaging.
/// Ensures strict compliance with MCP schemas (e.g. omitting nulls so optional fields satisfy Zod validation).
/// </summary>
public static class McpJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };
}

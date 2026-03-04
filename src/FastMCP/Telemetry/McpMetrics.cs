namespace FastMCP.Telemetry;

/// <summary>
/// Constants for FastMCP metrics and tracing.
/// </summary>
public static class McpMetrics
{
    public const string MeterName = "FastMCP";
    public const string ActivitySourceName = "FastMCP";

    // Metric Names
    public const string ToolInvocations = "mcp.tool.invocations";
    public const string ToolDuration = "mcp.tool.duration";
    public const string ToolErrors = "mcp.tool.errors";
    public const string PromptRequests = "mcp.prompt.requests";
    public const string ResourceReads = "mcp.resource.reads";
}

namespace FastMCP.Telemetry;

/// <summary>
/// Configuration options for FastMCP telemetry.
/// </summary>
public class McpTelemetryOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether metrics collection is enabled.
    /// Default is true.
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether distributed tracing is enabled.
    /// Default is true.
    /// </summary>
    public bool EnableTracing { get; set; } = true;

    /// <summary>
    /// Gets or sets the service name used for telemetry attributes.
    /// Default is "fastmcp-server".
    /// </summary>
    public string ServiceName { get; set; } = "fastmcp-server";

    /// <summary>
    /// Gets or sets the service version used for telemetry attributes.
    /// Default is "1.0.0".
    /// </summary>
    public string ServiceVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Gets or sets a value indicating whether to include tool input arguments in trace tags.
    /// WARNING: Enabling this may log Sensitive PII. Use with caution.
    /// Default is false.
    /// </summary>
    public bool IncludeToolInputs { get; set; } = false;
}

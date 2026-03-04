using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace FastMCP.Telemetry;

/// <summary>
/// Manages OpenTelemetry instrumentation for FastMCP.
/// </summary>
public class McpInstrumentation : IDisposable
{
    public ActivitySource ActivitySource { get; }
    public Meter Meter { get; }

    public Counter<long> ToolInvocations { get; }
    public Histogram<double> ToolDuration { get; }
    public Counter<long> ToolErrors { get; }
    public Counter<long> PromptRequests { get; }
    public Counter<long> ResourceReads { get; }

    public McpInstrumentation(McpTelemetryOptions options)
    {
        string version = typeof(McpInstrumentation).Assembly.GetName().Version?.ToString() ?? "1.0.0";
        
        ActivitySource = new ActivitySource(McpMetrics.ActivitySourceName, version);
        Meter = new Meter(McpMetrics.MeterName, version);

        ToolInvocations = Meter.CreateCounter<long>(McpMetrics.ToolInvocations, description: "Number of tool invocations");
        ToolDuration = Meter.CreateHistogram<double>(McpMetrics.ToolDuration, unit: "ms", description: "Duration of tool execution");
        ToolErrors = Meter.CreateCounter<long>(McpMetrics.ToolErrors, description: "Number of tool execution errors");
        PromptRequests = Meter.CreateCounter<long>(McpMetrics.PromptRequests, description: "Number of prompt requests");
        ResourceReads = Meter.CreateCounter<long>(McpMetrics.ResourceReads, description: "Number of resource read requests");
    }

    public void Dispose()
    {
        ActivitySource.Dispose();
        Meter.Dispose();
    }
}

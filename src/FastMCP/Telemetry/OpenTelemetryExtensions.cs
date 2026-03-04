using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using FastMCP.Telemetry;

namespace Microsoft.Extensions.DependencyInjection;

public static class FastMcpOpenTelemetryExtensions
{
    /// <summary>
    /// Adds FastMCP instrumentation to the TracerProvider.
    /// </summary>
    public static TracerProviderBuilder AddMcpInstrumentation(this TracerProviderBuilder builder)
    {
        return builder.AddSource(McpMetrics.ActivitySourceName);
    }

    /// <summary>
    /// Adds FastMCP instrumentation to the MeterProvider.
    /// </summary>
    public static MeterProviderBuilder AddMcpInstrumentation(this MeterProviderBuilder builder)
    {
        return builder.AddMeter(McpMetrics.MeterName);
    }
}

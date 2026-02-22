// ============================================================
// TelemetryDemo - FastMCP Observability Example
// ============================================================
// This example shows how to enable OpenTelemetry metrics and
// distributed tracing on a FastMCP server with one line:
//
//   builder.WithTelemetry(t => { t.ServiceName = "telemetry-demo"; });
//
// Metrics auto-tracked:
//   - mcp.tool.invocations  (counter, tagged by tool.name)
//   - mcp.tool.duration     (histogram in ms, tagged by tool.name)
//   - mcp.tool.errors       (counter, tagged by tool.name)
//   - mcp.prompt.requests   (counter)
//   - mcp.resource.reads    (counter)
//
// How to validate:
//   1. Run this server: dotnet run
//   2. In another terminal, use dotnet-counters to watch live metrics:
//      dotnet-counters monitor -n TelemetryDemo --counters FastMCP
//   3. Call a tool via the MCP test endpoint (see below)
//   4. Watch metrics increment in real-time in the console exporter output
// ============================================================

using FastMCP.Attributes;
using FastMCP.Hosting;
using FastMCP.Server;
using FastMCP.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using System.Reflection;

// ─── 1. Create the FastMCP server ───────────────────────────
var server = new FastMCPServer("TelemetryDemo", version: "1.0.0");
var builder = McpServerBuilder.Create(server, args);

// ─── 2. Register tools from this assembly ───────────────────
builder.WithComponentsFrom(Assembly.GetExecutingAssembly());

// ─── 3. ✨ Enable FastMCP Telemetry (one line!) ──────────────
builder.WithTelemetry(telemetry =>
{
    telemetry.ServiceName    = "telemetry-demo-server";
    telemetry.ServiceVersion = "1.0.0";
    telemetry.EnableMetrics  = true;
    telemetry.EnableTracing  = true;
    // telemetry.IncludeToolInputs = false; // Keep false - may log PII
});

// ─── 4. Configure OpenTelemetry exporters (consumer's choice) 
//        This is the STANDARD OTel pattern — FastMCP plugs into it.
builder.GetWebAppBuilder().Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(
            serviceName: "telemetry-demo-server",
            serviceVersion: "1.0.0"))
    .WithMetrics(metrics =>
    {
        // This is the FastMCP extension method on MeterProviderBuilder
        metrics.AddMcpInstrumentation();

        // Export to console for development/validation
        metrics.AddConsoleExporter();

        // For production, replace/add:
        // metrics.AddPrometheusExporter();
        // (requires: OpenTelemetry.Exporter.Prometheus.AspNetCore)
    })
    .WithTracing(tracing =>
    {
        // This is the FastMCP extension method on TracerProviderBuilder
        tracing.AddMcpInstrumentation();

        // Export traces to console for development/validation
        tracing.AddConsoleExporter();

        // For production, replace/add:
        // tracing.AddOtlpExporter();
        // tracing.AddAzureMonitorTraceExporter();
    });

// ─── 5. Build & Run ─────────────────────────────────────────
var app = builder.Build();
Console.Error.WriteLine("[TelemetryDemo] Server starting...");
Console.Error.WriteLine("[TelemetryDemo] Metrics exported to console (look for 'FastMCP' meter)");
Console.Error.WriteLine("[TelemetryDemo] Run in another terminal: dotnet-counters monitor -n TelemetryDemo --counters FastMCP");
await app.RunMcpAsync(args);

// ============================================================
// MCP Tools — designed to exercise all telemetry metrics
// ============================================================
public static class TelemetryDemoTools
{
    /// <summary>
    /// Fast tool — good for watching mcp.tool.invocations and mcp.tool.duration
    /// </summary>
    [McpTool(Description = "Returns a greeting. Call this repeatedly to watch invocation counters grow.")]
    public static string Greet(string name)
    {
        Console.Error.WriteLine($"[Tool] Greet called with name='{name}'");
        return $"Hello, {name}! (Tracked by FastMCP Telemetry)";
    }

    /// <summary>
    /// Slow tool — good for watching mcp.tool.duration histogram
    /// </summary>
    [McpTool(Description = "Simulates slow work. Use to observe duration histograms in telemetry.")]
    public static async Task<string> SlowOperation(int delayMs = 500)
    {
        Console.Error.WriteLine($"[Tool] SlowOperation started (delay={delayMs}ms)");
        await Task.Delay(Math.Min(delayMs, 5000)); // Cap at 5s for safety
        Console.Error.WriteLine($"[Tool] SlowOperation completed");
        return $"Completed after {delayMs}ms delay.";
    }

    /// <summary>
    /// Failing tool — good for watching mcp.tool.errors counter
    /// </summary>
    [McpTool(Description = "Always throws. Use to verify mcp.tool.errors counter increments.")]
    public static string AlwaysFails(string reason = "test error")
    {
        Console.Error.WriteLine($"[Tool] AlwaysFails called — about to throw");
        throw new InvalidOperationException($"Intentional failure: {reason}");
    }

    /// <summary>
    /// Concurrent tool — call multiple times to stress-test and see 
    /// that counters are thread-safe and accumulate correctly.
    /// </summary>
    [McpTool(Description = "Returns a random number with a simulated variable delay.")]
    public static async Task<string> RandomDelay()
    {
        var rng = Random.Shared;
        int delay = rng.Next(50, 800);
        await Task.Delay(delay);
        return $"Done in {delay}ms";
    }
}

// ─── MCP Resource — to exercise mcp.resource.reads ──────────
public static class TelemetryDemoResources
{
    [McpResource(Uri = "telemetry://status", Description = "Returns current server status")]
    public static string GetStatus()
    {
        return $"TelemetryDemo server is running. UTC: {DateTime.UtcNow:O}";
    }
}

// ─── MCP Prompt — to exercise mcp.prompt.requests ───────────
public static class TelemetryDemoPrompts
{
    [McpPrompt(Name = "analyze-metrics", Description = "Prompt to ask an AI to analyze the metrics")]
    public static string AnalyzeMetrics(string focus = "errors")
    {
        return $"You are an SRE. Analyze the following FastMCP server telemetry, paying special attention to {focus}. " +
               $"Look for anomalies, spikes in error rates, and unusually high tool durations.";
    }
}

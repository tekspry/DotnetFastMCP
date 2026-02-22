using System.Diagnostics;
using FastMCP.Protocol;
using FastMCP.Telemetry;
using System.Text.Json;

namespace FastMCP.Hosting;

public class McpTelemetryMiddleware : IMcpMiddleware
{
    private readonly McpInstrumentation _instrumentation;
    private readonly McpTelemetryOptions _options;

    public McpTelemetryMiddleware(McpInstrumentation instrumentation, McpTelemetryOptions options)
    {
        _instrumentation = instrumentation;
        _options = options;
    }

    public async Task<JsonRpcResponse> InvokeAsync(McpMiddlewareContext context, McpMiddlewareDelegate next, CancellationToken cancellationToken)
    {
        var request = context.Request;
        
        // Only track interesting methods
        if (string.IsNullOrEmpty(request.Method))
        {
            return await next(context, cancellationToken);
        }

        string activityName = $"mcp.request.{request.Method}";
        using var activity = _options.EnableTracing 
            ? _instrumentation.ActivitySource.StartActivity(activityName) 
            : null;

        activity?.SetTag("mcp.method", request.Method);
        activity?.SetTag("mcp.request.id", request.Id);

        long startTime = Stopwatch.GetTimestamp();

        // Variables to hold tags for metrics later
        string? toolName = null;

        try
        {
            // Extract details for metrics/tracing before execution
            if (request.Method == "tools/call" && request.Params is JsonElement root && root.ValueKind == JsonValueKind.Object && root.TryGetProperty("name", out var nameProp))
            {
                toolName = nameProp.GetString();
                activity?.SetTag("mcp.tool.name", toolName);
                
                // We add the metric at the START or END? Usually invocation count is start.
                _instrumentation.ToolInvocations.Add(1, new KeyValuePair<string, object?>("tool.name", toolName));

                if (_options.IncludeToolInputs && root.TryGetProperty("arguments", out var argsProp))
                {
                    activity?.SetTag("mcp.tool.arguments", argsProp.ToString());
                }
            }
            else if (request.Method == "prompts/get")
            {
                _instrumentation.PromptRequests.Add(1);
            }
            else if (request.Method == "resources/read")
            {
                _instrumentation.ResourceReads.Add(1);
            }

            var response = await next(context, cancellationToken);

            if (response.Error != null)
            {
                activity?.SetStatus(ActivityStatusCode.Error, response.Error.Message);
                activity?.SetTag("mcp.error.code", response.Error.Code);
                
                if (toolName != null)
                {
                    _instrumentation.ToolErrors.Add(1, new KeyValuePair<string, object?>("tool.name", toolName));
                }
            }
            else
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
            }

            return response;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            // Records exception as an ActivityEvent - follows OTel semantic conventions
            // (equivalent to what RecordException does internally)
            activity?.AddEvent(new ActivityEvent(
                "exception",
                tags: new ActivityTagsCollection
                {
                    { "exception.type", ex.GetType().FullName ?? ex.GetType().Name },
                    { "exception.message", ex.Message },
                    { "exception.stacktrace", ex.StackTrace ?? string.Empty }
                }));
            throw;
        }
        finally
        {
            TimeSpan duration = Stopwatch.GetElapsedTime(startTime);
            if (toolName != null)
            {
                 _instrumentation.ToolDuration.Record(duration.TotalMilliseconds, new KeyValuePair<string, object?>("tool.name", toolName));
            }
        }
    }
}

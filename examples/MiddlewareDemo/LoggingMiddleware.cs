using FastMCP.Hosting;
using FastMCP.Protocol;
using System.Text.Json;

namespace MiddlewareDemo;

// 1. Define Middleware
// This middleware logs every request and response to Console.Error (Stderr)
// Stderr is safe to use even in Stdio mode (unlike Console.Out)
public class LoggingMiddleware : IMcpMiddleware
{
    private int _requestCount = 0;

    public async Task<JsonRpcResponse> InvokeAsync(McpMiddlewareContext context, McpMiddlewareDelegate next, CancellationToken ct)
    {
        _requestCount++;
        Console.Error.WriteLine($"[🔍 MIDDLEWARE] #{_requestCount} Incoming: {method} (ID: {id})");

        // Example: Inspect/Modify context or request here if needed

        // Call the next middleware/handler in the pipeline
        var response = await next(context, ct);

        // Inspect response
        if (response.Error != null)
        {
            Console.Error.WriteLine($"[❌ MIDDLEWARE] #{_requestCount} Error: {response.Error.Message}");
        }
        else
        {
            // Serialize result for preview
            string resultJson = JsonSerializer.Serialize(response.Result);
            if (resultJson.Length > 50) resultJson = resultJson.Substring(0, 47) + "...";
            Console.Error.WriteLine($"[✅ MIDDLEWARE] #{_requestCount} Success: {resultJson}");
        }

        return response;
    }
}

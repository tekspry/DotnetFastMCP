using FastMCP.Protocol;

namespace FastMCP.Hosting;

/// <summary>
/// Defines middleware that can intercept JSON-RPC messages.
/// </summary>
public interface IMcpMiddleware
{
    /// <summary>
    /// Invokes the middleware logic.
    /// </summary>
    /// <param name="context">The context for the current request.</param>
    /// <param name="next">The delegate to call the next middleware in the pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The JSON-RPC response.</returns>
    Task<JsonRpcResponse> InvokeAsync(McpMiddlewareContext context, McpMiddlewareDelegate next, CancellationToken cancellationToken);
}
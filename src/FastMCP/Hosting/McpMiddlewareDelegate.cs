using FastMCP.Protocol;

namespace FastMCP.Hosting;

/// <summary>
/// Represents a function that processes an MCP request.
/// </summary>
public delegate Task<JsonRpcResponse> McpMiddlewareDelegate(McpMiddlewareContext context, CancellationToken cancellationToken);
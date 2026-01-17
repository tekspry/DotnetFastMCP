using System.Security.Claims;
using FastMCP.Protocol;
using FastMCP.Server;

namespace FastMCP.Hosting;

/// <summary>
/// Encapsulates the context for an MCP request within the middleware pipeline.
/// </summary>
public class McpMiddlewareContext
{
    public FastMCPServer Server { get; }
    public JsonRpcRequest Request { get; }
    public ClaimsPrincipal? User { get; }
    public IMcpSession? Session { get; }

    public McpMiddlewareContext(FastMCPServer server, JsonRpcRequest request, ClaimsPrincipal? user, IMcpSession? session)
    {
        Server = server;
        Request = request;
        User = user;
        Session = session;
    }
}
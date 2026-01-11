using System.Threading.Tasks;
namespace FastMCP.Protocol;
/// <summary>
/// Represents an active MCP session capable of sending notifications to the client.
/// </summary>
public interface IMcpSession
{
    /// <summary>
    /// Sends a JSON-RPC notification to the client.
    /// </summary>
    Task SendNotificationAsync(string method, object? parameters, CancellationToken cancellationToken = default);
}
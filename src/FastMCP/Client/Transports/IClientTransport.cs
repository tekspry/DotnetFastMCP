using System.Threading;
using System.Threading.Tasks;

namespace FastMCP.Client.Transports;

/// <summary>
/// Defines the contract for an MCP client transport.
/// responsible for sending requests and receiving responses/notifications.
/// </summary>
public interface IClientTransport : IAsyncDisposable
{
    /// <summary>
    /// Establishes the connection to the server.
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a JSON-RPC message (request or notification).
    /// </summary>
    Task SendAsync(object message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the next JSON-RPC message from the stream.
    /// </summary>
    Task<string?> ReadNextMessageAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns true if the transport connection is active.
    /// </summary>
    bool IsConnected { get; }
}
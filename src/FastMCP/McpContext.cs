using FastMCP.Protocol;
using FastMCP.Storage;

namespace FastMCP;
/// <summary>
/// Provides access to the current request context and session interactions (logging, progress).
/// </summary>
public class McpContext
{
    private readonly IMcpSession _session;
    
    /// <summary>
    /// The ID of the current request.
    /// </summary>
    public object RequestId { get; }
    /// <summary>
    /// The cancellation token for the current request.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Access to persistent storage.
    /// </summary>
    public IMcpStorage Storage { get; } 
    public McpContext(IMcpSession session, object requestId, CancellationToken cancellationToken, IMcpStorage storage)
    {
        _session = session;
        RequestId = requestId;
        CancellationToken = cancellationToken;
        Storage = storage;
    }
    /// <summary>
    /// Sends a log message to the client.
    /// </summary>
    public Task LogAsync(McpLogLevel level, string message, string? logger = null)
    {
        return _session.SendNotificationAsync("notifications/message", new 
        {
             level = level.ToString().ToLower(),
             logger = "tool",
             data = message
        }, CancellationToken);
    }
    
    public Task LogErrorAsync(string message, string? logger = null) => LogAsync(McpLogLevel.Error, message, logger);
    public Task LogWarningAsync(string message, string? logger = null) => LogAsync(McpLogLevel.Warning, message, logger);
    public Task LogInfoAsync(string message, string? logger = null) => LogAsync(McpLogLevel.Info, message, logger);
    public Task LogDebugAsync(string message, string? logger = null) => LogAsync(McpLogLevel.Debug, message, logger);
    /// <summary>
    /// Reports progress for a long-running operation.
    /// </summary>
    public async Task ReportProgressAsync(double progress, double? total = null)
    {
        await _session.SendNotificationAsync("notifications/progress", new
        {
            progressToken = RequestId, // Using RequestId as implicit token for simplicity, or could be separate
            progress,
            total
        }, CancellationToken);
    }
}
public enum McpLogLevel
{
    Debug,
    Info,
    Warning,
    Error
}
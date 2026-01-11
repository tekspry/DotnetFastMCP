using FastMCP.Protocol;
using Microsoft.Extensions.Logging;

namespace FastMCP.Hosting;

public class ServerLogSession : IMcpSession
{
    private readonly ILogger _logger;
    public ServerLogSession(ILogger logger) => _logger = logger;
    public Task SendNotificationAsync(string method, object? parameters, CancellationToken cancellationToken)
    {
        // For HTTP, we can't 'Send' to client asynchronously during request.
        // We log it to the server logs for visibility.
        if (method == "notifications/message" && parameters is System.Text.Json.JsonElement je)
        {
             // Try to extract content for cleaner logs if possible
             _logger.LogInformation("Tool Notification [{Method}]: {Params}", method, parameters);
        }
        else 
        {
             _logger.LogInformation("Tool Notification [{Method}]", method);
        }
        return Task.CompletedTask;
    }
}
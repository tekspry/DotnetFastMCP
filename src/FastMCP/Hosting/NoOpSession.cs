using FastMCP.Protocol;

namespace FastMCP.Hosting;

internal class NoOpSession : IMcpSession
{
    public Task SendNotificationAsync(string method, object? parameters, CancellationToken cancellationToken) 
    {
        return Task.CompletedTask; 
    }
}
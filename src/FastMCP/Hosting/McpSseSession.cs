using FastMCP.Protocol;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace FastMCP.Hosting;

/// <summary>
/// Represents an active SSE session for a specific client.
/// </summary>
public class McpSseSession : IMcpSession
{
    private readonly HttpResponse _response;
    private readonly JsonSerializerOptions _jsonOptions;
    
    public string Id { get; }

    public McpSseSession(HttpResponse response, JsonSerializerOptions jsonOptions)
    {
        Id = Guid.NewGuid().ToString();
        _response = response;
        _jsonOptions = jsonOptions;
    }

    /// <summary>
    /// Sends a JSON-RPC notification to the client via SSE.
    /// </summary>
    public async Task SendNotificationAsync(string method, object? parameters, CancellationToken cancellationToken = default)
    {
        var notification = new JsonRpcNotification
        {
            JsonRpc = "2.0",
            Method = method,
            Params = parameters
        };
        await SendSseEventAsync("message", notification, cancellationToken);
    }

    /// <summary>
    /// Sends a standard JSON-RPC response (result/error) via SSE.
    /// Used because POST requests in SSE mode do not return the result in the HTTP body.
    /// </summary>
    public async Task SendResponseAsync(JsonRpcResponse response, CancellationToken cancellationToken)
    {
        await SendSseEventAsync("message", response, cancellationToken);
    }

    /// <summary>
    /// Sends the initial 'endpoint' event telling the client where to POST messages.
    /// </summary>
    public async Task SendEndpointEventAsync(string postUri, CancellationToken cancellationToken)
    {
        await SendSseEventAsync("endpoint", postUri, cancellationToken);
    }

    private async Task SendSseEventAsync(string eventType, object data, CancellationToken cancellationToken)
    {
        // specific SSE format:
        // event: type\n
        // data: json\n\n
        
        await _response.WriteAsync($"event: {eventType}\n", cancellationToken);
        
        await _response.WriteAsync("data: ", cancellationToken);
        await JsonSerializer.SerializeAsync(_response.Body, data, _jsonOptions, cancellationToken);
        await _response.WriteAsync("\n\n", cancellationToken);
        
        await _response.Body.FlushAsync(cancellationToken);
    }
}
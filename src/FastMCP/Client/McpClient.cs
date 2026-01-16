using FastMCP.Client.Transports;
using FastMCP.Protocol;
using System.Collections.Concurrent;
using System.Text.Json;

namespace FastMCP.Client;

public class McpClient : IAsyncDisposable
{
    private readonly IClientTransport _transport;
    private readonly ConcurrentDictionary<object, TaskCompletionSource<JsonRpcResponse>> _pendingRequests = new();
    private int _requestIdCounter = 0;
    private Task? _readLoopTask;
    private readonly CancellationTokenSource _cts = new();

    public event Action<string, object?>? OnNotification;

    public McpClient(IClientTransport transport)
    {
        _transport = transport;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _transport.ConnectAsync(cancellationToken);
        _readLoopTask = ProcessIncomingMessagesAsync();
    }

    public async Task<ListToolsResult> ListToolsAsync(CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<ListToolsResult>("tools/list", null, cancellationToken);
    }

    public async Task<ListResourcesResult> ListResourcesAsync(CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<ListResourcesResult>("resources/list", null, cancellationToken);
    }

    public async Task<TResult> CallToolAsync<TResult>(string toolName, object arguments, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<TResult>("tools/call", new { name = toolName, arguments }, cancellationToken);
    }

    private async Task<T> SendRequestAsync<T>(string method, object? parameters, CancellationToken cancellationToken)
    {
        var id = Interlocked.Increment(ref _requestIdCounter);
        var tcs = new TaskCompletionSource<JsonRpcResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        
        _pendingRequests.TryAdd(id, tcs);

        var request = new JsonRpcRequest
        {
            JsonRpc = "2.0",
            Method = method,
            Params = parameters,
            Id = id
        };

        try
        {
            await _transport.SendAsync(request, cancellationToken);
            var response = await tcs.Task.WaitAsync(cancellationToken);

            if (response.Error != null)
            {
                throw new Exception($"MCP Error {response.Error.Code}: {response.Error.Message}");
            }

            if (response.Result is JsonElement element)
            {
                return element.Deserialize<T>(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })!;
            }
            
            // Handle primitives or direct casts if needed
            return (T)response.Result!;
        }
        finally
        {
            _pendingRequests.TryRemove(id, out _);
        }
    }

    private async Task ProcessIncomingMessagesAsync()
    {
        while (!_cts.Token.IsCancellationRequested && _transport.IsConnected)
        {
            try
            {
                var message = await _transport.ReadNextMessageAsync(_cts.Token);
                if (message == null) break;

                // Determine if it's a response or notification
                using var doc = JsonDocument.Parse(message);
                
                if (doc.RootElement.TryGetProperty("id", out var idProp) && idProp.ValueKind != JsonValueKind.Null)
                {
                    // It's a Response
                    if (idProp.ValueKind == JsonValueKind.Number && _pendingRequests.TryGetValue(idProp.GetInt32(), out var tcs))
                    {
                        var response = JsonSerializer.Deserialize<JsonRpcResponse>(message, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                        if (response != null) tcs.TrySetResult(response);
                    }
                }
                else
                {
                    // It's a Notification
                    var notification = JsonSerializer.Deserialize<JsonRpcNotification>(message, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
                    if (notification != null)
                    {
                        OnNotification?.Invoke(notification.Method, notification.Params);
                    }
                }
            }
            catch (Exception)
            {
                // Log or handle read errors
                if (_cts.Token.IsCancellationRequested) break;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        if (_transport != null) await _transport.DisposeAsync();
    }
}
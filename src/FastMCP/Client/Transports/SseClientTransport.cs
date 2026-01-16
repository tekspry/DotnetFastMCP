using System.Net.Http.Json;
using System.Text.Json;

namespace FastMCP.Client.Transports;

public class SseClientTransport : IClientTransport
{
    private readonly string _sseUrl;
    private readonly HttpClient _httpClient;
    private Stream? _sseStream;
    private StreamReader? _reader;
    private string? _postUrl;

    public bool IsConnected => _sseStream != null;

    public SseClientTransport(string url)
    {
        _sseUrl = url;
        _httpClient = new HttpClient();
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(_sseUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        _sseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        _reader = new StreamReader(_sseStream);
    }

    public async Task<string?> ReadNextMessageAsync(CancellationToken cancellationToken = default)
    {
        if (_reader == null) return null;

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await _reader.ReadLineAsync(cancellationToken);
            if (line == null) return null;

            if (line.StartsWith("event: endpoint"))
            {
                // Next line is data: ...
                var dataLine = await _reader.ReadLineAsync(cancellationToken);
                var endpoint = dataLine?.Substring("data: ".Length).Trim('"');
                // Construct absolute URL if relative
                if (!string.IsNullOrEmpty(endpoint))
                {
                     // Assuming endpoint comes as /message?sessionId=...
                     var uri = new Uri(_sseUrl);
                     _postUrl = $"{uri.Scheme}://{uri.Authority}{endpoint}";
                }
            }
            else if (line.StartsWith("data: "))
            {
                // This is a message or notification
                return line.Substring("data: ".Length);
            }
        }
        return null;
    }

    public async Task SendAsync(object message, CancellationToken cancellationToken = default)
    {
        if (_postUrl == null) throw new InvalidOperationException("Endpoint not yet discovered. Wait for connection.");
        
        await _httpClient.PostAsJsonAsync(_postUrl, message, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        _reader?.Dispose();
        _sseStream?.Dispose();
        _httpClient.Dispose();
    }
}
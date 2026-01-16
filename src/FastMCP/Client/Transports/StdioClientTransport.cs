using System.Diagnostics;
using System.Text.Json;

namespace FastMCP.Client.Transports;

public class StdioClientTransport : IClientTransport
{
    private readonly ProcessStartInfo _startInfo;
    private Process? _process;
    private StreamReader? _reader;
    private StreamWriter? _writer;

    public bool IsConnected => _process != null && !_process.HasExited;

    public StdioClientTransport(string command, string arguments)
    {
        _startInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
    }

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _process = Process.Start(_startInfo) ?? throw new Exception("Failed to start process");
        _reader = _process.StandardOutput;
        _writer = _process.StandardInput;
        return Task.CompletedTask;
    }

    public async Task SendAsync(object message, CancellationToken cancellationToken = default)
    {
        if (_writer == null) throw new InvalidOperationException("Not connected");
        
        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await _writer.WriteLineAsync(json.AsMemory(), cancellationToken);
        await _writer.FlushAsync(cancellationToken);
    }

    public async Task<string?> ReadNextMessageAsync(CancellationToken cancellationToken = default)
    {
        if (_reader == null) return null;
        return await _reader.ReadLineAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_process != null && !_process.HasExited)
        {
            _process.Kill();
            await _process.WaitForExitAsync();
        }
        _process?.Dispose();
    }
}
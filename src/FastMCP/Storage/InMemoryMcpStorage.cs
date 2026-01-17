using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;
namespace FastMCP.Storage;
/// <summary>
/// A thread-safe in-memory implementation of IMcpStorage.
/// Data is lost when the server restarts.
/// </summary>
public class InMemoryMcpStorage : IMcpStorage
{
    private readonly ConcurrentDictionary<string, string> _store = new();
    
    public Task<T?> GetAsync<T>(string key)
    {
        if (_store.TryGetValue(key, out var json))
        {
            try 
            {
                return Task.FromResult(JsonSerializer.Deserialize<T>(json));
            }
            catch
            {
                return Task.FromResult<T?>(default);
            }
        }
        return Task.FromResult<T?>(default);
    }
    public Task SetAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        _store[key] = json;
        return Task.CompletedTask;
    }
    public Task DeleteAsync(string key)
    {
        _store.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
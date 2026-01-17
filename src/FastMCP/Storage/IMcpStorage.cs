using System.Threading.Tasks;
namespace FastMCP.Storage;
/// <summary>
/// Defines an abstraction for persistent key-value storage.
/// </summary>
public interface IMcpStorage
{
    /// <summary>
    /// Retrieves a value by key. Returns default(T) if not found.
    /// </summary>
    Task<T?> GetAsync<T>(string key);
    /// <summary>
    /// Sets a value for a given key. Overwrites if exists.
    /// </summary>
    Task SetAsync<T>(string key, T value);
    /// <summary>
    /// Deletes a value by key. Does nothing if key doesn't exist.
    /// </summary>
    Task DeleteAsync(string key);
}
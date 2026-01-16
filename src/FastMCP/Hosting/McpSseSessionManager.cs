using System.Collections.Concurrent;

namespace FastMCP.Hosting;

public class McpSseSessionManager
{
    private readonly ConcurrentDictionary<string, McpSseSession> _sessions = new();

    public void AddSession(McpSseSession session)
    {
        _sessions.TryAdd(session.Id, session);
    }

    public void RemoveSession(string id)
    {
        _sessions.TryRemove(id, out _);
    }

    public McpSseSession? GetSession(string id)
    {
        _sessions.TryGetValue(id, out var session);
        return session;
    }
}
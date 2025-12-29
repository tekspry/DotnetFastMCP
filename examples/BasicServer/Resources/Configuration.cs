using FastMCP.Attributes;

namespace BasicServer.Resources;

/// <summary>
/// Application configuration resources.
/// </summary>
public static class Configuration
{
    /// <summary>
    /// Provides the application's static configuration.
    /// </summary>
    [McpResource("resource://config")]
    public static object GetConfig()
    {
        return new 
        { 
            Version = "1.0.0", 
            Author = "DotnetFastMCP Team",
            Environment = "Development"
        };
    }
}

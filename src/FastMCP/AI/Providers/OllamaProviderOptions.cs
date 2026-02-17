namespace FastMCP.AI.Providers;

/// <summary>
/// Configuration options for Ollama LLM provider.
/// </summary>
public class OllamaProviderOptions
{
    /// <summary>
    /// Base URL for the Ollama API (default: http://localhost:11434).
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:11434";

    /// <summary>
    /// Default model to use if not specified in generation options.
    /// </summary>
    public string DefaultModel { get; set; } = "llama3.1:8b";

    /// <summary>
    /// Request timeout in seconds (default: 120).
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;
}

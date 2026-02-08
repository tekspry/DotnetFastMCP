namespace FastMCP.AI.Providers;

/// <summary>
/// Configuration options for OpenAI LLM provider.
/// </summary>
public class OpenAIProviderOptions
{
    /// <summary>
    /// OpenAI API key (required). Should be loaded from secure configuration.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for OpenAI API (default: https://api.openai.com).
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.openai.com";

    /// <summary>
    /// Default model to use (default: gpt-3.5-turbo).
    /// </summary>
    public string DefaultModel { get; set; } = "gpt-3.5-turbo";

    /// <summary>
    /// Request timeout in seconds (default: 60).
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Organization ID (optional).
    /// </summary>
    public string? OrganizationId { get; set; }
}

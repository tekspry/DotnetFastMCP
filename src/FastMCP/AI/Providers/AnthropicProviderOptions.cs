namespace FastMCP.AI.Providers;

/// <summary>
/// Configuration options for Anthropic Claude provider.
/// </summary>
public class AnthropicProviderOptions
{
    /// <summary>
    /// Base URL for Anthropic API.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.anthropic.com";
    
    /// <summary>
    /// API key from platform.claude.com (starts with sk-ant-).
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Default model to use. Recommended: claude-opus-4.6 or claude-sonnet-4.5.
    /// </summary>
    public string DefaultModel { get; set; } = "claude-sonnet-4.5";
    
    /// <summary>
    /// API version header value.
    /// </summary>
    public string ApiVersion { get; set; } = "2023-06-01";
    
    /// <summary>
    /// Request timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;
    
    /// <summary>
    /// Optional: Specify inference geography (e.g., "us" for US-only).
    /// Adds 1.1x pricing multiplier.
    /// </summary>
    public string? InferenceGeo { get; set; }
}

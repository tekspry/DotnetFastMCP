namespace FastMCP.AI.Providers;

/// <summary>
/// Configuration options for Google Gemini provider.
/// </summary>
public class GeminiProviderOptions
{
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com";
    
    /// <summary>
    /// API key from Google AI Studio.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Default model. Recommended: gemini-3-flash or gemini-3-pro.
    /// </summary>
    public string DefaultModel { get; set; } = "gemini-3-flash";
    
    /// <summary>
    /// API version (v1beta recommended for latest features).
    /// </summary>
    public string ApiVersion { get; set; } = "v1beta";
    
    public int TimeoutSeconds { get; set; } = 60;
}

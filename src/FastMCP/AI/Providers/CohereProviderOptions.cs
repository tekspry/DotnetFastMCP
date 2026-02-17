namespace FastMCP.AI.Providers;

public class CohereProviderOptions
{
    public string BaseUrl { get; set; } = "https://api.cohere.ai";
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Default model. Use command-a for latest capabilities.
    /// </summary>
    public string DefaultModel { get; set; } = "command-a";
    
    /// <summary>
    /// API version (v2 recommended).
    /// </summary>
    public string ApiVersion { get; set; } = "v2";
    
    public int TimeoutSeconds { get; set; } = 60;
}

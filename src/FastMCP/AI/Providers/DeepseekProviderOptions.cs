namespace FastMCP.AI.Providers;

public class DeepseekProviderOptions
{
    public string BaseUrl { get; set; } = "https://api.deepseek.com";
    
    /// <summary>
    /// API key from platform.deepseek.com
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Default model. Use deepseek-chat for speed, deepseek-reasoner for reasoning.
    /// </summary>
    public string DefaultModel { get; set; } = "deepseek-chat";
    
    public int TimeoutSeconds { get; set; } = 60;
}

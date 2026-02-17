namespace FastMCP.AI.Providers;

public class HuggingFaceProviderOptions
{
    /// <summary>
    /// Base URL. Use serverless API or custom Inference Endpoint URL.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api-inference.huggingface.co";
    
    /// <summary>
    /// API token from huggingface.co/settings/tokens
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Default model ID (e.g., "meta-llama/Llama-3.1-8B-Instruct").
    /// </summary>
    public string DefaultModel { get; set; } = "mistralai/Mistral-7B-Instruct-v0.3";
    
    /// <summary>
    /// Timeout (HF can be slower for cold starts).
    /// </summary>
    public int TimeoutSeconds { get; set; } = 120;
}

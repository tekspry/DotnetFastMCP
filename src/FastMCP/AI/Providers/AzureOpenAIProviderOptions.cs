namespace FastMCP.AI.Providers;

/// <summary>
/// Configuration options for Azure OpenAI LLM provider.
/// </summary>
public class AzureOpenAIProviderOptions
{
    /// <summary>
    /// Azure OpenAI resource endpoint (e.g., https://myresource.openai.azure.com).
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Azure OpenAI API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Deployment name (model deployment in Azure).
    /// </summary>
    public string DeploymentName { get; set; } = "gpt-35-turbo";

    /// <summary>
    /// API version to use (default: 2024-02-15-preview).
    /// </summary>
    public string ApiVersion { get; set; } = "2024-02-15-preview";

    /// <summary>
    /// Request timeout in seconds (default: 60).
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;
}

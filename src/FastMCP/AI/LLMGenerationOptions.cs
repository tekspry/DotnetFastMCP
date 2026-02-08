namespace FastMCP.AI;

/// <summary>
/// Options for controlling LLM text generation.
/// </summary>
public class LLMGenerationOptions
{
    /// <summary>
    /// The model to use for generation. If null, uses provider's default.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Controls randomness. Higher values (e.g., 0.8) make output more random,
    /// lower values (e.g., 0.2) make it more deterministic.
    /// </summary>
    public float? Temperature { get; set; }

    /// <summary>
    /// Maximum number of tokens to generate.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Top-p sampling: nucleus sampling parameter.
    /// </summary>
    public float? TopP { get; set; }

    /// <summary>
    /// Sequences where the API will stop generating further tokens.
    /// </summary>
    public string[]? StopSequences { get; set; }

    /// <summary>
    /// System prompt/instruction for chat models.
    /// </summary>
    public string? SystemPrompt { get; set; }
}

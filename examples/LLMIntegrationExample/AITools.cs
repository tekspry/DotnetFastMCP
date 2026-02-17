using FastMCP.AI;
using FastMCP.Attributes;
using Microsoft.Extensions.Logging;

namespace LLMIntegrationExample;

/// <summary>
/// Example MCP server tools that demonstrate LLM provider integration.
/// </summary>
public class AITools
{
    // Static fields to hold service instances
    private static ILLMProvider? _llm;
    private static ILogger<AITools>? _logger;

    /// <summary>
    /// Initialize static services (called from Program.cs)
    /// </summary>
    public static void Initialize(ILLMProvider llm, ILogger<AITools> logger)
    {
        _llm = llm;
        _logger = logger;
    }

    /// <summary>
    /// Generates a creative story based on the given prompt using the configured LLM.
    /// </summary>
    /// <param name="prompt">The story prompt or theme</param>
    /// <param name="maxTokens">Maximum tokens for the story (default: 500)</param>
    /// <returns>A creative story</returns>
    [McpTool("generate_story")]
    public static async Task<string> GenerateStory(string prompt, int maxTokens = 500)
    {
        _logger!.LogInformation("Generating story for prompt: {Prompt}", prompt);

        var systemPrompt = "You are a creative storyteller. Write engaging, imaginative stories.";
        
        var result = await _llm!.GenerateAsync(prompt, new LLMGenerationOptions
        {
            SystemPrompt = systemPrompt,
            Temperature = 0.8f,
            MaxTokens = maxTokens
        });

        return result;
    }

    /// <summary>
    /// Summarizes the given text using the configured LLM.
    /// </summary>
    /// <param name="text">The text to summarize</param>
    /// <param name="sentenceCount">Number of sentences in summary (default: 3)</param>
    /// <returns>A concise summary</returns>
    [McpTool("summarize_text")]
    public static async Task<string> SummarizeText(string text, int sentenceCount = 3)
    {
        _logger!.LogInformation("Summarizing text (length: {Length} chars)", text.Length);

        var prompt = $"Summarize the following text in exactly {sentenceCount} sentences:\n\n{text}";
        
        var result = await _llm!.GenerateAsync(prompt, new LLMGenerationOptions
        {
            Temperature = 0.3f, // Lower temperature for more focused summaries
            MaxTokens = 200
        });

        return result;
    }

    /// <summary>
    /// Analyzes sentiment of the given text and provides a detailed explanation.
    /// </summary>
    /// <param name="text">The text to analyze</param>
    /// <returns>Sentiment analysis result with explanation</returns>
    [McpTool("analyze_sentiment")]
    public static async Task<string> AnalyzeSentiment(string text)
    {
        _logger!.LogInformation("Analyzing sentiment for text");

        var prompt = $@"Analyze the sentiment of the following text. Provide:
1. Overall sentiment (Positive/Negative/Neutral)
2. Confidence score (0-100%)
3. Brief explanation

Text: {text}";
        
        var result = await _llm!.GenerateAsync(prompt, new LLMGenerationOptions
        {
            Temperature = 0.2f, // Very low temperature for analytical tasks
            MaxTokens = 150
        });

        return result;
    }

    /// <summary>
    /// Translates text from one language to another using the LLM.
    /// </summary>
    /// <param name="text">The text to translate</param>
    /// <param name="targetLanguage">Target language (e.g., "Spanish", "French", "Japanese")</param>
    /// <returns>Translated text</returns>
    [McpTool("translate_text")]
    public static async Task<string> TranslateText(string text, string targetLanguage)
    {
        _logger!.LogInformation("Translating to {Language}", targetLanguage);

        var prompt = $"Translate the following text to {targetLanguage}. Only provide the translation, no explanations:\n\n{text}";
        
        var result = await _llm!.GenerateAsync(prompt, new LLMGenerationOptions
        {
            Temperature = 0.3f,
            MaxTokens = 500
        });

        return result;
    }

    /// <summary>
    /// Has a conversation with the AI using chat format (maintains context).
    /// </summary>
    /// <param name="userMessage">User's message</param>
    /// <param name="systemPrompt">Optional system prompt to set AI behavior</param>
    /// <returns>AI's response</returns>
    [McpTool("chat")]
    public static async Task<string> Chat(string userMessage, string? systemPrompt = null)
    {
        _logger!.LogInformation("Processing chat message");

        var messages = new List<LLMMessage>();
        
        if (!string.IsNullOrEmpty(systemPrompt))
        {
            messages.Add(LLMMessage.System(systemPrompt));
        }
        else
        {
            messages.Add(LLMMessage.System("You are a helpful AI assistant."));
        }
        
        messages.Add(LLMMessage.User(userMessage));

        var result = await _llm!.ChatAsync(messages, new LLMGenerationOptions
        {
            Temperature = 0.7f,
            MaxTokens = 300
        });

        return result;
    }

    /// <summary>
    /// Generates code in the specified programming language based on the description.
    /// </summary>
    /// <param name="description">Description of what the code should do</param>
    /// <param name="language">Programming language (e.g., "C#", "Python", "JavaScript")</param>
    /// <returns>Generated code with explanations</returns>
    [McpTool("generate_code")]
    public static async Task<string> GenerateCode(string description, string language)
    {
        _logger!.LogInformation("Generating {Language} code", language);

        var systemPrompt = $"You are an expert {language} programmer. Write clean, well-documented code with comments explaining key sections.";
        var prompt = $"Write {language} code for: {description}";

        var result = await _llm!.GenerateAsync(prompt, new LLMGenerationOptions
        {
            SystemPrompt = systemPrompt,
            Temperature = 0.4f,
            MaxTokens = 800
        });

        return result;
    }
}

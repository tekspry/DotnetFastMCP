using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace FastMCP.AI.Providers;

/// <summary>
/// Google Gemini provider implementation (Feb 2026 API).
/// Supports Gemini 3 Pro, Flash, and Deep Think models.
/// </summary>
public class GeminiProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly GeminiProviderOptions _options;
    private readonly ILogger<GeminiProvider> _logger;

    public GeminiProvider(
        HttpClient httpClient,
        GeminiProviderOptions options,
        ILogger<GeminiProvider> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task<string> GenerateAsync(
        string prompt,
        LLMGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var model = options?.Model ?? _options.DefaultModel;
        
        var contents = new List<object>
        {
            new { parts = new[] { new { text = prompt } } }
        };
        
        // System instruction (Gemini's native format)
        object? systemInstruction = null;
        if (!string.IsNullOrEmpty(options?.SystemPrompt))
        {
            systemInstruction = new { parts = new[] { new { text = options.SystemPrompt } } };
        }

        var requestBody = new
        {
            contents,
            systemInstruction,
            generationConfig = new
            {
                temperature = options?.Temperature,
                maxOutputTokens = options?.MaxTokens,
                topP = options?.TopP,
                stopSequences = options?.StopSequences
            }
        };

        _logger.LogDebug("Sending request to Gemini API with model {Model}", model);

        var response = await _httpClient.PostAsJsonAsync(
            $"/{_options.ApiVersion}/models/{model}:generateContent",
            requestBody,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(
            cancellationToken: cancellationToken);

        return result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;
    }

    public async IAsyncEnumerable<string> StreamAsync(
        string prompt,
        LLMGenerationOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var model = options?.Model ?? _options.DefaultModel;
        
        var contents = new List<object>
        {
            new { parts = new[] { new { text = prompt } } }
        };

        object? systemInstruction = null;
        if (!string.IsNullOrEmpty(options?.SystemPrompt))
        {
            systemInstruction = new { parts = new[] { new { text = options.SystemPrompt } } };
        }

        var requestBody = new
        {
            contents,
            systemInstruction,
            generationConfig = new
            {
                temperature = options?.Temperature,
                maxOutputTokens = options?.MaxTokens
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"/{_options.ApiVersion}/models/{model}:streamGenerateContent",
            requestBody,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var chunk = JsonSerializer.Deserialize<GeminiStreamChunk>(line);
            var text = chunk?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
            if (text != null)
            {
                yield return text;
            }            
        }
    }

    public async Task<string> ChatAsync(
        IEnumerable<LLMMessage> messages,
        LLMGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var model = options?.Model ?? _options.DefaultModel;
        
        // Gemini uses "model" role instead of "assistant"
        var contents = messages.Select(m => new
        {
            role = m.Role == "assistant" ? "model" : "user",
            parts = new[] { new { text = m.Content } }
        }).ToList();

        var requestBody = new
        {
            contents,
            generationConfig = new
            {
                temperature = options?.Temperature,
                maxOutputTokens = options?.MaxTokens
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"/{_options.ApiVersion}/models/{model}:generateContent",
            requestBody,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(
            cancellationToken: cancellationToken);

        return result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? string.Empty;
    }

    // Response DTOs
    private class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
    }

    private class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }

    private class Content
    {
        [JsonPropertyName("parts")]
        public List<Part>? Parts { get; set; }
    }

    private class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private class GeminiStreamChunk
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }
    }
}

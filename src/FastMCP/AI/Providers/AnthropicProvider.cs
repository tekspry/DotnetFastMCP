using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace FastMCP.AI.Providers;

/// <summary>
/// Anthropic Claude provider implementation (Feb 2026 API).
/// Supports Claude Opus 4.6, Sonnet 4.5, and Haiku 4.5.
/// </summary>
public class AnthropicProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly AnthropicProviderOptions _options;
    private readonly ILogger<AnthropicProvider> _logger;

    public AnthropicProvider(
        HttpClient httpClient,
        AnthropicProviderOptions options,
        ILogger<AnthropicProvider> logger)
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
        
        var messages = new List<object>();
        
        // System prompt as separate parameter (Claude's native format)
        string? systemPrompt = options?.SystemPrompt;
        
        messages.Add(new { role = "user", content = prompt });

        var requestBody = new
        {
            model,
            messages,
            system = systemPrompt, // Claude 4 native system parameter
            max_tokens = options?.MaxTokens ?? 1024,
            temperature = options?.Temperature,
            top_p = options?.TopP,
            stop_sequences = options?.StopSequences,
            metadata = _options.InferenceGeo != null 
                ? new { inference_geo = _options.InferenceGeo } 
                : null
        };

        _logger.LogDebug("Sending request to Anthropic API with model {Model}", model);

        var response = await _httpClient.PostAsJsonAsync(
            "/v1/messages",
            requestBody,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AnthropicResponse>(
            cancellationToken: cancellationToken);

        return result?.Content?.FirstOrDefault()?.Text ?? string.Empty;
    }

    public async IAsyncEnumerable<string> StreamAsync(
        string prompt,
        LLMGenerationOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var model = options?.Model ?? _options.DefaultModel;
        
        var messages = new List<object>
        {
            new { role = "user", content = prompt }
        };

        var requestBody = new
        {
            model,
            messages,
            system = options?.SystemPrompt,
            max_tokens = options?.MaxTokens ?? 1024,
            temperature = options?.Temperature,
            stream = true,
            metadata = _options.InferenceGeo != null 
                ? new { inference_geo = _options.InferenceGeo } 
                : null
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/v1/messages",
            requestBody,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;

            var jsonData = line.Substring(6);
            if (jsonData == "[DONE]") break;

            var chunk = JsonSerializer.Deserialize<AnthropicStreamChunk>(jsonData);
            if (chunk?.Delta?.Text != null)
            {
                yield return chunk.Delta.Text;
            }            
        }
    }

    public async Task<string> ChatAsync(
        IEnumerable<LLMMessage> messages,
        LLMGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var model = options?.Model ?? _options.DefaultModel;
        
        var anthropicMessages = messages.Select(m => new
        {
            role = m.Role,
            content = m.Content
        }).ToList();

        var requestBody = new
        {
            model,
            messages = anthropicMessages,
            system = options?.SystemPrompt,
            max_tokens = options?.MaxTokens ?? 1024,
            temperature = options?.Temperature,
            top_p = options?.TopP,
            metadata = _options.InferenceGeo != null 
                ? new { inference_geo = _options.InferenceGeo } 
                : null
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/v1/messages",
            requestBody,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AnthropicResponse>(
            cancellationToken: cancellationToken);

        return result?.Content?.FirstOrDefault()?.Text ?? string.Empty;
    }

    // Response DTOs
    private class AnthropicResponse
    {
        [JsonPropertyName("content")]
        public List<ContentBlock>? Content { get; set; }
    }

    private class ContentBlock
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private class AnthropicStreamChunk
    {
        [JsonPropertyName("delta")]
        public Delta? Delta { get; set; }
    }

    private class Delta
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}

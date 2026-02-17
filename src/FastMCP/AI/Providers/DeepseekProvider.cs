using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace FastMCP.AI.Providers;

/// <summary>
/// Deepseek provider implementation (Feb 2026, OpenAI-compatible API).
/// Supports DeepSeek-V3.2 models with thinking and non-thinking modes.
/// </summary>
public class DeepseekProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly DeepseekProviderOptions _options;
    private readonly ILogger<DeepseekProvider> _logger;

    public DeepseekProvider(
        HttpClient httpClient,
        DeepseekProviderOptions options,
        ILogger<DeepseekProvider> logger)
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
        
        if (!string.IsNullOrEmpty(options?.SystemPrompt))
        {
            messages.Add(new { role = "system", content = options.SystemPrompt });
        }
        
        messages.Add(new { role = "user", content = prompt });

        var requestBody = new
        {
            model,
            messages,
            temperature = options?.Temperature,
            max_tokens = options?.MaxTokens,
            top_p = options?.TopP,
            stop = options?.StopSequences
        };

        _logger.LogDebug("Sending request to Deepseek API with model {Model}", model);

        var response = await _httpClient.PostAsJsonAsync(
            "/v1/chat/completions",
            requestBody,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<DeepseekResponse>(
            cancellationToken: cancellationToken);

        return result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
    }

    public async IAsyncEnumerable<string> StreamAsync(
        string prompt,
        LLMGenerationOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var model = options?.Model ?? _options.DefaultModel;
        
        var messages = new List<object>();
        if (!string.IsNullOrEmpty(options?.SystemPrompt))
        {
            messages.Add(new { role = "system", content = options.SystemPrompt });
        }
        messages.Add(new { role = "user", content = prompt });

        var requestBody = new
        {
            model,
            messages,
            temperature = options?.Temperature,
            max_tokens = options?.MaxTokens,
            stream = true
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/v1/chat/completions",
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

            var chunk = JsonSerializer.Deserialize<DeepseekStreamChunk>(jsonData);
            var content = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;
            if (content != null)
            {
                yield return content;
            }
        }
    }

    public async Task<string> ChatAsync(
        IEnumerable<LLMMessage> messages,
        LLMGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var model = options?.Model ?? _options.DefaultModel;
        
        var deepseekMessages = messages.Select(m => new
        {
            role = m.Role,
            content = m.Content
        }).ToList();

        var requestBody = new
        {
            model,
            messages = deepseekMessages,
            temperature = options?.Temperature,
            max_tokens = options?.MaxTokens
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/v1/chat/completions",
            requestBody,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<DeepseekResponse>(
            cancellationToken: cancellationToken);

        return result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
    }

    // Response DTOs (OpenAI-compatible)
    private class DeepseekResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }
    }

    private class Choice
    {
        [JsonPropertyName("message")]
        public Message? Message { get; set; }
    }

    private class Message
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    private class DeepseekStreamChunk
    {
        [JsonPropertyName("choices")]
        public List<StreamChoice>? Choices { get; set; }
    }

    private class StreamChoice
    {
        [JsonPropertyName("delta")]
        public Delta? Delta { get; set; }
    }

    private class Delta
    {
        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }
}

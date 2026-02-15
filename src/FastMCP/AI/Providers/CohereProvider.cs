using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace FastMCP.AI.Providers;

/// <summary>
/// Cohere provider implementation (API V2, Feb 2026).
/// Supports Command A, Command R+, and other Cohere models.
/// </summary>
public class CohereProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly CohereProviderOptions _options;
    private readonly ILogger<CohereProvider> _logger;

    public CohereProvider(
        HttpClient httpClient,
        CohereProviderOptions options,
        ILogger<CohereProvider> logger)
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
        
        var requestBody = new
        {
            model,
            message = prompt,
            preamble = options?.SystemPrompt, // Cohere's term for system prompt
            temperature = options?.Temperature,
            max_tokens = options?.MaxTokens,
            p = options?.TopP,
            stop_sequences = options?.StopSequences
        };

        _logger.LogDebug("Sending request to Cohere API V2 with model {Model}", model);

        var response = await _httpClient.PostAsJsonAsync(
            $"/{_options.ApiVersion}/chat",
            requestBody,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CohereResponse>(
            cancellationToken: cancellationToken);

        return result?.Text ?? string.Empty;
    }

    public async IAsyncEnumerable<string> StreamAsync(
        string prompt,
        LLMGenerationOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var model = options?.Model ?? _options.DefaultModel;
        
        var requestBody = new
        {
            model,
            message = prompt,
            preamble = options?.SystemPrompt,
            temperature = options?.Temperature,
            max_tokens = options?.MaxTokens,
            stream = true
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"/{_options.ApiVersion}/chat",
            requestBody,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var chunk = JsonSerializer.Deserialize<CohereStreamChunk>(line);
            if (chunk?.EventType == "text-generation" && chunk.Text != null)
            {
                yield return chunk.Text;
            }
        }
    }

    public async Task<string> ChatAsync(
        IEnumerable<LLMMessage> messages,
        LLMGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var model = options?.Model ?? _options.DefaultModel;
        
        var messageList = messages.ToList();
        var lastMessage = messageList.LastOrDefault();
        
        // Cohere V2 expects chat history (all except last) + current message
        var chatHistory = messageList.Take(messageList.Count - 1).Select(m => new
        {
            role = m.Role.ToUpper(), // Cohere uses uppercase: USER, CHATBOT
            message = m.Content
        }).ToList();

        var requestBody = new
        {
            model,
            message = lastMessage?.Content ?? "",
            chat_history = chatHistory,
            preamble = options?.SystemPrompt,
            temperature = options?.Temperature,
            max_tokens = options?.MaxTokens
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"/{_options.ApiVersion}/chat",
            requestBody,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CohereResponse>(
            cancellationToken: cancellationToken);

        return result?.Text ?? string.Empty;
    }

    // Response DTOs
    private class CohereResponse
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private class CohereStreamChunk
    {
        [JsonPropertyName("event_type")]
        public string? EventType { get; set; }
        
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}

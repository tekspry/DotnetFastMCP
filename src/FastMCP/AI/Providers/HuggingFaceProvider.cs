using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace FastMCP.AI.Providers;

/// <summary>
/// Hugging Face Inference provider (Feb 2026).
/// Supports Inference Providers and custom Inference Endpoints.
/// </summary>
public class HuggingFaceProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly HuggingFaceProviderOptions _options;
    private readonly ILogger<HuggingFaceProvider> _logger;

    public HuggingFaceProvider(
        HttpClient httpClient,
        HuggingFaceProviderOptions options,
        ILogger<HuggingFaceProvider> logger)
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
        
        // Prepend system prompt if provided
        var fullPrompt = prompt;
        if (!string.IsNullOrEmpty(options?.SystemPrompt))
        {
            fullPrompt = $"{options.SystemPrompt}\n\n{prompt}";
        }
        
        var requestBody = new
        {
            inputs = fullPrompt,
            parameters = new
            {
                temperature = options?.Temperature,
                max_new_tokens = options?.MaxTokens,
                top_p = options?.TopP,
                stop = options?.StopSequences,
                return_full_text = false // Only return generated text
            }
        };

        _logger.LogDebug("Sending request to Hugging Face with model {Model}", model);

        var response = await _httpClient.PostAsJsonAsync(
            $"/models/{model}",
            requestBody,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<List<HuggingFaceResponse>>(
            cancellationToken: cancellationToken);

        return result?.FirstOrDefault()?.GeneratedText ?? string.Empty;
    }

    public async IAsyncEnumerable<string> StreamAsync(
        string prompt,
        LLMGenerationOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var model = options?.Model ?? _options.DefaultModel;
        
        var fullPrompt = prompt;
        if (!string.IsNullOrEmpty(options?.SystemPrompt))
        {
            fullPrompt = $"{options.SystemPrompt}\n\n{prompt}";
        }
        
        var requestBody = new
        {
            inputs = fullPrompt,
            parameters = new
            {
                temperature = options?.Temperature,
                max_new_tokens = options?.MaxTokens,
                return_full_text = false
            },
            stream = true
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"/models/{model}",
            requestBody,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var chunk = JsonSerializer.Deserialize<HuggingFaceStreamChunk>(line);
            if (chunk?.Token?.Text != null)
            {
                yield return chunk.Token.Text;
            }
        }
    }

    public async Task<string> ChatAsync(
        IEnumerable<LLMMessage> messages,
        LLMGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // HF doesn't have universal chat format - convert to prompt
        var prompt = string.Join("\n\n", messages.Select(m => $"{m.Role}: {m.Content}"));
        return await GenerateAsync(prompt, options, cancellationToken);
    }

    // Response DTOs
    private class HuggingFaceResponse
    {
        [JsonPropertyName("generated_text")]
        public string? GeneratedText { get; set; }
    }

    private class HuggingFaceStreamChunk
    {
        [JsonPropertyName("token")]
        public Token? Token { get; set; }
    }

    private class Token
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}

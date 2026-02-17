using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FastMCP.AI.Providers;

/// <summary>
/// LLM provider for Ollama (local model hosting).
/// </summary>
public class OllamaProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly OllamaProviderOptions _options;
    private readonly ILogger<OllamaProvider> _logger;

    public OllamaProvider(
        HttpClient httpClient,
        OllamaProviderOptions options,
        ILogger<OllamaProvider> logger)
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
        
        // If SystemPrompt is provided, prepend it to the prompt
        var fullPrompt = prompt;
        if (!string.IsNullOrEmpty(options?.SystemPrompt))
        {
            fullPrompt = $"{options.SystemPrompt}\n\n{prompt}";
        }
        
        var requestBody = new
        {
            model,
            prompt = fullPrompt,
            stream = false,
            options = new
            {
                temperature = options?.Temperature,
                num_predict = options?.MaxTokens,
                top_p = options?.TopP,
                stop = options?.StopSequences
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/api/generate",
            requestBody,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(
            cancellationToken: cancellationToken);

        return result?.Response ?? string.Empty;
    }

    public async IAsyncEnumerable<string> StreamAsync(
        string prompt,
        LLMGenerationOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var model = options?.Model ?? _options.DefaultModel;
        
        // If SystemPrompt is provided, prepend it to the prompt
        var fullPrompt = prompt;
        if (!string.IsNullOrEmpty(options?.SystemPrompt))
        {
            fullPrompt = $"{options.SystemPrompt}\n\n{prompt}";
        }
        
        var requestBody = new
        {
            model,
            prompt = fullPrompt,
            stream = true,
            options = new
            {
                temperature = options?.Temperature,
                num_predict = options?.MaxTokens,
                top_p = options?.TopP,
                stop = options?.StopSequences
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/api/generate",
            requestBody,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new System.IO.StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            var chunk = JsonSerializer.Deserialize<OllamaStreamChunk>(line);
            if (chunk?.Response != null)
            {
                yield return chunk.Response;
            }

            if (chunk?.Done == true) break;
        }
    }

    public async Task<string> ChatAsync(
        IEnumerable<LLMMessage> messages,
        LLMGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var model = options?.Model ?? _options.DefaultModel;
        var requestBody = new
        {
            model,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }),
            stream = false,
            options = new
            {
                temperature = options?.Temperature,
                num_predict = options?.MaxTokens,
                top_p = options?.TopP,
                stop = options?.StopSequences
            }
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/api/chat",
            requestBody,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(
            cancellationToken: cancellationToken);

        return result?.Message?.Content ?? string.Empty;
    }

    // Internal DTOs for Ollama API
    private class OllamaGenerateResponse
    {
        public string? Response { get; set; }
    }

    private class OllamaStreamChunk
    {
        public string? Response { get; set; }
        public bool Done { get; set; }
    }

    private class OllamaChatResponse
    {
        public OllamaMessage? Message { get; set; }
    }

    private class OllamaMessage
    {
        public string? Content { get; set; }
    }
}

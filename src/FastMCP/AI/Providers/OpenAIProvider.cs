using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FastMCP.AI.Providers;

/// <summary>
/// LLM provider for OpenAI (GPT models).
/// </summary>
public class OpenAIProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly OpenAIProviderOptions _options;
    private readonly ILogger<OpenAIProvider> _logger;

    public OpenAIProvider(
        HttpClient httpClient,
        OpenAIProviderOptions options,
        ILogger<OpenAIProvider> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;

        // Set authorization header with API key
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public async Task<string> GenerateAsync(
        string prompt,
        LLMGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Convert simple prompt to chat format
        var messages = new List<LLMMessage> { LLMMessage.User(prompt) };
        if (options?.SystemPrompt != null)
        {
            messages.Insert(0, LLMMessage.System(options.SystemPrompt));
        }

        return await ChatAsync(messages, options, cancellationToken);
    }

    public async IAsyncEnumerable<string> StreamAsync(
        string prompt,
        LLMGenerationOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var model = options?.Model ?? _options.DefaultModel;
        var messages = new List<object> { new { role = "user", content = prompt } };
        if (options?.SystemPrompt != null)
        {
            messages.Insert(0, new { role = "system", content = options.SystemPrompt });
        }

        var requestBody = new
        {
            model,
            messages,
            stream = true,
            temperature = options?.Temperature,
            max_tokens = options?.MaxTokens,
            top_p = options?.TopP,
            stop = options?.StopSequences
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/v1/chat/completions",
            requestBody,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new System.IO.StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;
            if (line.Contains("[DONE]")) break;

            var json = line.Substring(6); // Remove "data: " prefix
            var chunk = JsonSerializer.Deserialize<OpenAIStreamChunk>(json);
            var content = chunk?.Choices?.FirstOrDefault()?.Delta?.Content;

            if (!string.IsNullOrEmpty(content))
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
        var requestBody = new
        {
            model,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }),
            temperature = options?.Temperature,
            max_tokens = options?.MaxTokens,
            top_p = options?.TopP,
            stop = options?.StopSequences
        };

        var response = await _httpClient.PostAsJsonAsync(
            "/v1/chat/completions",
            requestBody,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>(
            cancellationToken: cancellationToken);

        return result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
    }

    // Internal DTOs for OpenAI API
    private class OpenAIChatResponse
    {
        public Choice[]? Choices { get; set; }
    }

    private class Choice
    {
        public Message? Message { get; set; }
    }

    private class Message
    {
        public string? Content { get; set; }
    }

    private class OpenAIStreamChunk
    {
        public StreamChoice[]? Choices { get; set; }
    }

    private class StreamChoice
    {
        public Delta? Delta { get; set; }
    }

    private class Delta
    {
        public string? Content { get; set; }
    }
}

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
/// LLM provider for Azure OpenAI Service.
/// </summary>
public class AzureOpenAIProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly AzureOpenAIProviderOptions _options;
    private readonly ILogger<AzureOpenAIProvider> _logger;

    public AzureOpenAIProvider(
        HttpClient httpClient,
        AzureOpenAIProviderOptions options,
        ILogger<AzureOpenAIProvider> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;

        // Azure OpenAI uses different auth header
        _httpClient.DefaultRequestHeaders.Add("api-key", _options.ApiKey);
    }

    public async Task<string> GenerateAsync(
        string prompt,
        LLMGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
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
        var messages = new List<object> { new { role = "user", content = prompt } };
        if (options?.SystemPrompt != null)
        {
            messages.Insert(0, new { role = "system", content = options.SystemPrompt });
        }

        var deployment = options?.Model ?? _options.DeploymentName;
        var endpoint = $"/openai/deployments/{deployment}/chat/completions?api-version={_options.ApiVersion}";

        var requestBody = new
        {
            messages,
            stream = true,
            temperature = options?.Temperature,
            max_tokens = options?.MaxTokens,
            top_p = options?.TopP,
            stop = options?.StopSequences
        };

        var response = await _httpClient.PostAsJsonAsync(endpoint, requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new System.IO.StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ")) continue;
            if (line.Contains("[DONE]")) break;

            var json = line.Substring(6);
            var chunk = JsonSerializer.Deserialize<AzureOpenAIStreamChunk>(json);
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
        var deployment = options?.Model ?? _options.DeploymentName;
        var endpoint = $"/openai/deployments/{deployment}/chat/completions?api-version={_options.ApiVersion}";

        var requestBody = new
        {
            messages = messages.Select(m => new { role = m.Role, content = m.Content }),
            temperature = options?.Temperature,
            max_tokens = options?.MaxTokens,
            top_p = options?.TopP,
            stop = options?.StopSequences
        };

        var response = await _httpClient.PostAsJsonAsync(endpoint, requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AzureOpenAIChatResponse>(
            cancellationToken: cancellationToken);

        return result?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
    }

    // Internal DTOs (same structure as OpenAI)
    private class AzureOpenAIChatResponse
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

    private class AzureOpenAIStreamChunk
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

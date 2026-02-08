using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FastMCP.AI;

/// <summary>
/// Base class for LLM provider implementations with common functionality.
/// Custom providers should inherit from this class.
/// </summary>
public abstract class BaseLLMProvider : ILLMProvider
{
    protected readonly HttpClient HttpClient;
    protected readonly ILogger Logger;
    protected readonly JsonSerializerOptions JsonOptions;

    protected BaseLLMProvider(HttpClient httpClient, ILogger logger)
    {
        HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Default JSON options for most providers
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Generates a text completion. Default implementation converts to chat format.
    /// Override if provider has a dedicated completion endpoint.
    /// </summary>
    public virtual async Task<string> GenerateAsync(
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

    /// <summary>
    /// Generates a streaming text completion. Default implementation converts to chat format.
    /// Override if provider has a dedicated completion endpoint.
    /// </summary>
    public virtual async IAsyncEnumerable<string> StreamAsync(
        string prompt,
        LLMGenerationOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messages = new List<LLMMessage> { LLMMessage.User(prompt) };
        if (options?.SystemPrompt != null)
        {
            messages.Insert(0, LLMMessage.System(options.SystemPrompt));
        }

        await foreach (var chunk in StreamChatAsync(messages, options, cancellationToken))
        {
            yield return chunk;
        }
    }

    /// <summary>
    /// Abstract method for chat completions. Must be implemented by derived classes.
    /// </summary>
    public abstract Task<string> ChatAsync(
        IEnumerable<LLMMessage> messages,
        LLMGenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Virtual method for streaming chat. Override to provide streaming support.
    /// Default implementation throws NotSupportedException.
    /// </summary>
    public virtual IAsyncEnumerable<string> StreamChatAsync(
        IEnumerable<LLMMessage> messages,
        LLMGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException(
            $"{GetType().Name} does not support streaming. Override StreamChatAsync to add support.");
    }

    /// <summary>
    /// Helper method to log and handle HTTP errors with provider context.
    /// </summary>
    protected async Task<HttpResponseMessage> SendRequestAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogDebug("Sending {Method} request to {Uri}", request.Method, request.RequestUri);
            var response = await HttpClient.SendAsync(request, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                Logger.LogError(
                    "LLM provider request failed with status {StatusCode}: {Error}",
                    response.StatusCode,
                    errorContent);
            }

            response.EnsureSuccessStatusCode();
            return response;
        }
        catch (HttpRequestException ex)
        {
            Logger.LogError(ex, "HTTP request to LLM provider failed");
            throw new LLMProviderException("Failed to communicate with LLM provider", ex);
        }
        catch (TaskCanceledException ex)
        {
            Logger.LogWarning("LLM provider request timed out");
            throw new LLMProviderException("Request to LLM provider timed out", ex);
        }
    }

    /// <summary>
    /// Helper method to parse streaming response lines (SSE format).
    /// </summary>
    protected async IAsyncEnumerable<string> ParseStreamingResponse(
        HttpResponseMessage response,
        Func<string, string?> extractContent,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new System.IO.StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Skip SSE comment lines
            if (line.StartsWith(":")) continue;

            // Handle SSE data lines
            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6);
                if (data.Trim() == "[DONE]") break;

                var content = extractContent(data);
                if (!string.IsNullOrEmpty(content))
                {
                    yield return content;
                }
            }
        }
    }
}

/// <summary>
/// Exception thrown when LLM provider operations fail.
/// </summary>
public class LLMProviderException : Exception
{
    public LLMProviderException(string message) : base(message) { }
    public LLMProviderException(string message, Exception innerException) 
        : base(message, innerException) { }
}

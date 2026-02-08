using FastMCP.AI.Providers;
using FastMCP.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using System;

namespace FastMCP.AI;

/// <summary>
/// Extension methods for registering LLM providers with McpServerBuilder.
/// </summary>
public static class LLMProviderExtensions
{
    /// <summary>
    /// Adds Ollama as the LLM provider.
    /// </summary>
    public static McpServerBuilder AddOllamaProvider(
        this McpServerBuilder builder,
        Action<OllamaProviderOptions>? configureOptions = null)
    {
        var services = builder.GetWebAppBuilder().Services;
        var configuration = builder.GetWebAppBuilder().Configuration;

        // Bind configuration
        var options = new OllamaProviderOptions();
        configuration.GetSection("AI:Ollama").Bind(options);
        configureOptions?.Invoke(options);

        // Validate options
        if (string.IsNullOrEmpty(options.BaseUrl))
        {
            throw new InvalidOperationException("Ollama BaseUrl is required");
        }

        // Register options
        services.AddSingleton(options);

        // Register HttpClient with Polly retry policy
        services.AddHttpClient<OllamaProvider>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        })
        .AddPolicyHandler(GetRetryPolicy());

        // Register provider as singleton
        services.AddSingleton<ILLMProvider, OllamaProvider>();

        return builder;
    }

    /// <summary>
    /// Adds OpenAI as the LLM provider.
    /// </summary>
    public static McpServerBuilder AddOpenAIProvider(
        this McpServerBuilder builder,
        Action<OpenAIProviderOptions>? configureOptions = null)
    {
        var services = builder.GetWebAppBuilder().Services;
        var configuration = builder.GetWebAppBuilder().Configuration;

        // Bind configuration
        var options = new OpenAIProviderOptions();
        configuration.GetSection("AI:OpenAI").Bind(options);
        configureOptions?.Invoke(options);

        // Validate options
        if (string.IsNullOrEmpty(options.ApiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API Key is required. Set it via configuration or pass it in configureOptions.");
        }

        // Register options
        services.AddSingleton(options);

        // Register HttpClient
        services.AddHttpClient<OpenAIProvider>(client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            if (!string.IsNullOrEmpty(options.OrganizationId))
            {
                client.DefaultRequestHeaders.Add("OpenAI-Organization", options.OrganizationId);
            }
        })
        .AddPolicyHandler(GetRetryPolicy());

        // Register provider as singleton
        services.AddSingleton<ILLMProvider, OpenAIProvider>();

        return builder;
    }

    /// <summary>
    /// Adds Azure OpenAI as the LLM provider.
    /// </summary>
    public static McpServerBuilder AddAzureOpenAIProvider(
        this McpServerBuilder builder,
        Action<AzureOpenAIProviderOptions>? configureOptions = null)
    {
        var services = builder.GetWebAppBuilder().Services;
        var configuration = builder.GetWebAppBuilder().Configuration;

        // Bind configuration
        var options = new AzureOpenAIProviderOptions();
        configuration.GetSection("AI:AzureOpenAI").Bind(options);
        configureOptions?.Invoke(options);

        // Validate options
        if (string.IsNullOrEmpty(options.Endpoint) || string.IsNullOrEmpty(options.ApiKey))
        {
            throw new InvalidOperationException(
                "Azure OpenAI Endpoint and ApiKey are required.");
        }

        // Register options
        services.AddSingleton(options);

        // Register HttpClient
        services.AddHttpClient<AzureOpenAIProvider>(client =>
        {
            client.BaseAddress = new Uri(options.Endpoint);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        })
        .AddPolicyHandler(GetRetryPolicy());

        // Register provider as singleton
        services.AddSingleton<ILLMProvider, AzureOpenAIProvider>();

        return builder;
    }

    /// <summary>
    /// Polly retry policy for transient HTTP errors.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    // Could add logging here
                });
    }
}

using FastMCP.AI.Providers;
using FastMCP.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

        // Register provider as singleton using factory to get the configured HttpClient
        services.AddSingleton<ILLMProvider>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(OllamaProvider));
            var logger = sp.GetRequiredService<ILogger<OllamaProvider>>();
            return new OllamaProvider(httpClient, options, logger);
        });

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

        // Register provider as singleton using factory
        services.AddSingleton<ILLMProvider>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(OpenAIProvider));
            var logger = sp.GetRequiredService<ILogger<OpenAIProvider>>();
            return new OpenAIProvider(httpClient, options, logger);
        });

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

        // Register provider as singleton using factory
        services.AddSingleton<ILLMProvider>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(AzureOpenAIProvider));
            var logger = sp.GetRequiredService<ILogger<AzureOpenAIProvider>>();
            return new AzureOpenAIProvider(httpClient, options, logger);
        });

        return builder;
    }

    /// <summary>
    /// Adds Anthropic Claude as the LLM provider (Feb 2026 API).
    /// Supports Claude Opus 4.6, Sonnet 4.5, and Haiku 4.5.
    /// </summary>
    /// <param name="builder">The MCP server builder.</param>
    /// <param name="configureOptions">Optional configuration action.</param>
    /// <returns>The builder for chaining.</returns>
    public static McpServerBuilder AddAnthropicProvider(
        this McpServerBuilder builder,
        Action<AnthropicProviderOptions>? configureOptions = null)
    {
        var services = builder.GetWebAppBuilder().Services;
        var configuration = builder.GetWebAppBuilder().Configuration;

        // Bind and configure options
        var options = new AnthropicProviderOptions();
        configuration.GetSection("AI:Anthropic").Bind(options);
        configureOptions?.Invoke(options);

        // Validate API key
        if (string.IsNullOrEmpty(options.ApiKey))
        {
            throw new InvalidOperationException(
                "Anthropic API Key is required. Get it from platform.claude.com");
        }

        services.AddSingleton(options);

        // Configure HttpClient with Anthropic-specific headers
        services.AddHttpClient(nameof(AnthropicProvider), client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("x-api-key", options.ApiKey);
            client.DefaultRequestHeaders.Add("anthropic-version", options.ApiVersion);
        })
        .AddPolicyHandler(GetRetryPolicy());

        // Register provider using factory pattern
        services.AddSingleton<ILLMProvider>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(AnthropicProvider));
            var logger = sp.GetRequiredService<ILogger<AnthropicProvider>>();
            return new AnthropicProvider(httpClient, options, logger);
        });

        return builder;
    }

    /// <summary>
    /// Adds Google Gemini as the LLM provider (Feb 2026 API).
    /// Supports Gemini 3 Pro, Flash, and Deep Think models.
    /// </summary>
    public static McpServerBuilder AddGeminiProvider(
        this McpServerBuilder builder,
        Action<GeminiProviderOptions>? configureOptions = null)
    {
        var services = builder.GetWebAppBuilder().Services;
        var configuration = builder.GetWebAppBuilder().Configuration;

        var options = new GeminiProviderOptions();
        configuration.GetSection("AI:Gemini").Bind(options);
        configureOptions?.Invoke(options);

        if (string.IsNullOrEmpty(options.ApiKey))
        {
            throw new InvalidOperationException(
                "Gemini API Key is required. Get it from Google AI Studio.");
        }

        services.AddSingleton(options);

        services.AddHttpClient(nameof(GeminiProvider), client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("x-goog-api-key", options.ApiKey);
        })
        .AddPolicyHandler(GetRetryPolicy());

        services.AddSingleton<ILLMProvider>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(GeminiProvider));
            var logger = sp.GetRequiredService<ILogger<GeminiProvider>>();
            return new GeminiProvider(httpClient, options, logger);
        });

        return builder;
    }

    /// <summary>
    /// Adds Cohere as the LLM provider (API V2, Feb 2026).
    /// Supports Command A, Command R+, and other Cohere models.
    /// </summary>
    public static McpServerBuilder AddCohereProvider(
        this McpServerBuilder builder,
        Action<CohereProviderOptions>? configureOptions = null)
    {
        var services = builder.GetWebAppBuilder().Services;
        var configuration = builder.GetWebAppBuilder().Configuration;

        var options = new CohereProviderOptions();
        configuration.GetSection("AI:Cohere").Bind(options);
        configureOptions?.Invoke(options);

        if (string.IsNullOrEmpty(options.ApiKey))
        {
            throw new InvalidOperationException(
                "Cohere API Key is required. Get it from cohere.com dashboard.");
        }

        services.AddSingleton(options);

        services.AddHttpClient(nameof(CohereProvider), client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
        })
        .AddPolicyHandler(GetRetryPolicy());

        services.AddSingleton<ILLMProvider>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(CohereProvider));
            var logger = sp.GetRequiredService<ILogger<CohereProvider>>();
            return new CohereProvider(httpClient, options, logger);
        });

        return builder;
    }

    /// <summary>
    /// Adds Hugging Face Inference as the LLM provider (Feb 2026).
    /// Supports Inference Providers and custom Inference Endpoints.
    /// </summary>
    public static McpServerBuilder AddHuggingFaceProvider(
        this McpServerBuilder builder,
        Action<HuggingFaceProviderOptions>? configureOptions = null)
    {
        var services = builder.GetWebAppBuilder().Services;
        var configuration = builder.GetWebAppBuilder().Configuration;

        var options = new HuggingFaceProviderOptions();
        configuration.GetSection("AI:HuggingFace").Bind(options);
        configureOptions?.Invoke(options);

        if (string.IsNullOrEmpty(options.ApiToken))
        {
            throw new InvalidOperationException(
                "Hugging Face API Token is required. Get it from huggingface.co/settings/tokens");
        }

        services.AddSingleton(options);

        services.AddHttpClient(nameof(HuggingFaceProvider), client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiToken}");
        })
        .AddPolicyHandler(GetRetryPolicy());

        services.AddSingleton<ILLMProvider>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(HuggingFaceProvider));
            var logger = sp.GetRequiredService<ILogger<HuggingFaceProvider>>();
            return new HuggingFaceProvider(httpClient, options, logger);
        });

        return builder;
    }

    /// <summary>
    /// Adds Deepseek as the LLM provider (Feb 2026, OpenAI-compatible).
    /// Supports DeepSeek-V3.2 with thinking and non-thinking modes.
    /// </summary>
    public static McpServerBuilder AddDeepseekProvider(
        this McpServerBuilder builder,
        Action<DeepseekProviderOptions>? configureOptions = null)
    {
        var services = builder.GetWebAppBuilder().Services;
        var configuration = builder.GetWebAppBuilder().Configuration;

        var options = new DeepseekProviderOptions();
        configuration.GetSection("AI:Deepseek").Bind(options);
        configureOptions?.Invoke(options);

        if (string.IsNullOrEmpty(options.ApiKey))
        {
            throw new InvalidOperationException(
                "Deepseek API Key is required. Get it from platform.deepseek.com");
        }

        services.AddSingleton(options);

        services.AddHttpClient(nameof(DeepseekProvider), client =>
        {
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
        })
        .AddPolicyHandler(GetRetryPolicy());

        services.AddSingleton<ILLMProvider>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(DeepseekProvider));
            var logger = sp.GetRequiredService<ILogger<DeepseekProvider>>();
            return new DeepseekProvider(httpClient, options, logger);
        });

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

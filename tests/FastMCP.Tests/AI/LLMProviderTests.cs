using System.Net;
using System.Net.Http;
using System.Text;
using FastMCP.AI.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FastMCP.Tests.AI;

public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_handler(request));
    }
}

public class LLMProviderTests
{
    [Fact]
    public async Task AnthropicProvider_GenerateAsync_ParsesResponseCorrectly()
    {
        var mockResponseJson = "{\"content\": [{\"type\": \"text\", \"text\": \"Hello from Claude!\"}]}";
        
        var handler = new MockHttpMessageHandler(req =>
        {
            Assert.Equal("/v1/messages", req.RequestUri?.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponseJson, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.com") };
        var options = new AnthropicProviderOptions { ApiKey = "test_key", DefaultModel = "claude-opus-4.6" };
        var provider = new AnthropicProvider(httpClient, options, NullLogger<AnthropicProvider>.Instance);

        var result = await provider.GenerateAsync("Hi");

        Assert.Equal("Hello from Claude!", result);
    }

    [Fact]
    public async Task GeminiProvider_GenerateAsync_ParsesResponseCorrectly()
    {
        var mockResponseJson = "{\"candidates\": [{\"content\": {\"parts\": [{\"text\": \"Hello from Gemini!\"}]}}]}";
        
        var handler = new MockHttpMessageHandler(req =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponseJson, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://generativelanguage.googleapis.com") };
        var options = new GeminiProviderOptions { ApiKey = "test_key", DefaultModel = "gemini-3-pro" };
        var provider = new GeminiProvider(httpClient, options, NullLogger<GeminiProvider>.Instance);

        var result = await provider.GenerateAsync("Hi");

        Assert.Equal("Hello from Gemini!", result);
    }

    [Fact]
    public async Task OpenAIProvider_GenerateAsync_ParsesResponseCorrectly()
    {
        var mockResponseJson = "{\"choices\": [{\"message\": {\"content\": \"Hello from GPT!\"}}]}";
        
        var handler = new MockHttpMessageHandler(req =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(mockResponseJson, Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com") };
        var options = new OpenAIProviderOptions { ApiKey = "test_key", DefaultModel = "gpt-4" };
        var provider = new OpenAIProvider(httpClient, options, NullLogger<OpenAIProvider>.Instance);

        var result = await provider.GenerateAsync("Hi");

        Assert.Equal("Hello from GPT!", result);
    }

    [Fact]
    public async Task AnthropicProvider_HttpError_ThrowsHttpRequestException()
    {
        var handler = new MockHttpMessageHandler(req =>
        {
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("{\"error\": \"Invalid API key\"}", Encoding.UTF8, "application/json")
            };
        });

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.com") };
        var options = new AnthropicProviderOptions { ApiKey = "invalid_key", DefaultModel = "claude-opus-4.6" };
        var provider = new AnthropicProvider(httpClient, options, NullLogger<AnthropicProvider>.Instance);

        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await provider.GenerateAsync("Hi");
        });
    }
}

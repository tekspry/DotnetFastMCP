using System.Reflection;
using FastMCP.Hosting;
using FastMCP.Server;
using FastMCP.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LLMIntegrationExample;

class Program
{
    static async Task Main(string[] args)
    {
        // Create FastMCP server
        var mcpServer = new FastMCPServer(name: "AI Tools Server");

        // Build the MCP server with LLM provider integration
        var builder = McpServerBuilder.Create(mcpServer, args);

        // Configure LLM Provider - Choose ONE of the following options:

        // Option 1: Ollama (Local, Free, Privacy-focused)
        // Requires Ollama running locally: https://ollama.ai
        builder.AddOllamaProvider(options =>
        {
            options.BaseUrl = "http://127.0.0.1:11434"; // Use 127.0.0.1 instead of localhost
            options.DefaultModel = "llama3.1:8b"; // Confirmed available
            options.TimeoutSeconds = 300; // Increase timeout for larger responses
        });

        // Option 2: OpenAI (Cloud, Requires API Key)
        /* Uncomment to use OpenAI:
        builder.AddOpenAIProvider(options =>
        {
            options.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") 
                ?? throw new Exception("OPENAI_API_KEY environment variable not set");
            options.DefaultModel = "gpt-3.5-turbo"; // or "gpt-4", "gpt-4-turbo"
        });
        */

        // Option 3: Azure OpenAI (Enterprise)
        /* Uncomment to use Azure OpenAI:
        builder.AddAzureOpenAIProvider(options =>
        {
            options.Endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!;
            options.ApiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!;
            options.DeploymentName = "gpt-35-turbo"; // Your deployment name
        });


        // Option 4: Anthropic (Enterprise)
        /* Uncomment to use Anthropic:
        builder.AddAnthropicProvider(options =>
        {
            options.ApiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")!;
            options.DefaultModel = "claude-opus-4.6"; // Latest model
            options.InferenceGeo = "us"; // Optional: US-only inference
        });

        // Option 5: Gemini (Enterprise)
        /* Uncomment to use Gemini:
        builder.AddGeminiProvider(options =>
        {
            options.ApiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")!;
            options.DefaultModel = "gemini-3-flash"; // Fast and cost-effective
        });

        // Option 6: Cohere (Enterprise)
        /* Uncomment to use Cohere:
        builder.AddCohereProvider(options =>
        {
            options.ApiKey = Environment.GetEnvironmentVariable("COHERE_API_KEY")!;
            options.DefaultModel = "command-a"; // Latest agentic model
        });

        // Option 7: Hugging Face (Cloud or Self-Hosted, Requires API Token)
        /* Uncomment to use Hugging Face:
        builder.AddHuggingFaceProvider(options =>
        {
            options.ApiToken = Environment.GetEnvironmentVariable("HF_TOKEN")!;
            options.DefaultModel = "meta-llama/Llama-3.1-8B-Instruct";
        });

        // Option 8: Deepseek (Enterprise, Specialized in Reasoning)
        /* Uncomment to use Deepseek:
        builder.AddDeepseekProvider(options =>
        {
        options.ApiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY")!;
        options.DefaultModel = "deepseek-reasoner"; // For complex reasoning
        });


        */

        // Register MCP tools from this assembly
        builder.WithComponentsFrom(Assembly.GetExecutingAssembly());

        // Build and run the server
        var app = builder.Build();
        
        // Initialize AITools with services from the DI container
        var llmProvider = app.Services.GetRequiredService<ILLMProvider>();
        var logger = app.Services.GetRequiredService<ILogger<AITools>>();
        AITools.Initialize(llmProvider, logger);
        
        Console.WriteLine("🚀 AI Tools MCP Server is starting...");
        Console.WriteLine($"   Provider: Ollama (llama3.1:8b)");
        Console.WriteLine($"   Available tools:");
        Console.WriteLine($"     - generate_story: Create creative stories");
        Console.WriteLine($"     - summarize_text: Summarize long text");
        Console.WriteLine($"     - analyze_sentiment: Analyze text sentiment");
        Console.WriteLine($"     - translate_text: Translate to other languages");
        Console.WriteLine($"     - chat: Have a conversation with AI");
        Console.WriteLine($"     - generate_code: Generate code in any language");
        Console.WriteLine();

        await app.RunMcpAsync(args);
    }
}

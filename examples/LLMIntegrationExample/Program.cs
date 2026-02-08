using System.Reflection;
using FastMCP.Hosting;
using FastMCP.Server;

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
            options.BaseUrl = "http://localhost:11434";
            options.DefaultModel = "llama3.1:8b"; // or "mistral", "phi3", etc.
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
        */

        // Register MCP tools from this assembly
        builder.WithComponentsFrom(Assembly.GetExecutingAssembly());

        // Build and run the server
        var app = builder.Build();
        
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

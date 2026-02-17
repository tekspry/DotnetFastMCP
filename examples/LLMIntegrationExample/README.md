# LLM Integration Example

This example demonstrates how to build an MCP server with AI capabilities using DotnetFastMCP's LLM Integration Plugin System.

## Features

This example includes 6 AI-powered MCP tools:

1. **generate_story** - Creates creative stories from prompts
2. **summarize_text** - Summarizes long text into key points
3. **analyze_sentiment** - Analyzes sentiment with confidence scores
4. **translate_text** - Translates text to any language
5. **chat** - Interactive AI assistant conversation
6. **generate_code** - Generates code in any programming language

## Supported LLM Providers

### Option 1: Ollama (Local, Free) ⭐ Recommended for Development

**Setup:**
1. Install Ollama from [https://ollama.ai](https://ollama.ai)
2. Pull a model: `ollama pull llama3.1:8b`
3. Ollama runs on `http://localhost:11434` by default
4. No API keys required!

**Why Ollama?**
- 🔒 Privacy: All processing happens locally
- 💰 Free: No API costs
- 🚀 Fast: Low latency for local inference
- 📦 Models: llama3.1, mistral, phi3, codellama, etc.

### Option 2: OpenAI (Cloud)

**Setup:**
1. Get API key from [https://platform.openai.com/api-keys](https://platform.openai.com/api-keys)
2. Set environment variable:
   ```bash
   # Windows PowerShell
   $env:OPENAI_API_KEY="sk-..."
   
   # Linux/Mac
   export OPENAI_API_KEY="sk-..."
   ```
3. Uncomment OpenAI configuration in `Program.cs`

**Models:** gpt-3.5-turbo, gpt-4, gpt-4-turbo

### Option 3: Azure OpenAI (Enterprise)

**Setup:**
1. Create Azure OpenAI resource
2. Deploy a model (e.g., gpt-35-turbo)
3. Set environment variables:
   ```bash
   $env:AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com/"
   $env:AZURE_OPENAI_API_KEY="your-key"
   ```
4. Uncomment Azure OpenAI configuration in `Program.cs`

## Running the Example

```bash
cd examples/LLMIntegrationExample
dotnet run
```

The server will start and expose MCP tools via SSE transport.

## Testing with MCP Client

You can test the tools using any MCP client. Examples:

### Using cURL (generate_story tool)

```bash
curl -X POST http://localhost:5000/sse \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "generate_story",
      "arguments": {
        "prompt": "A robot learning to feel emotions",
        "maxTokens": 300
      }
    },
    "id": 1
  }'
```

### Using Claude Desktop

Add to your Claude Desktop config:

```json
{
  "mcpServers": {
    "ai-tools": {
      "command": "dotnet",
      "args": ["run", "--project", "path/to/LLMIntegrationExample"],
      "env": {
        "OPENAI_KEY": "sk-..."
      }
    }
  }
}
```

## Configuration

Edit `appsettings.json` to configure providers:

```json
{
  "AI": {
    "Ollama": {
      "BaseUrl": "http://localhost:11434",
      "DefaultModel": "llama3.1:8b"
    }
  }
}
```

Or use environment variables (recommended for API keys):
- `AI__Ollama__BaseUrl`
- `OPENAI_API_KEY`
- `AZURE_OPENAI_ENDPOINT`
- `AZURE_OPENAI_API_KEY`

## Code Examples

### Simple Text Generation

```csharp
[McpTool("my_tool", "Description")]
public async Task<string> MyTool(string input)
{
    return await _llm.GenerateAsync(input, new LLMGenerationOptions
    {
        Temperature = 0.7f,
        MaxTokens = 200
    });
}
```

### Chat with Context

```csharp
var messages = new List<LLMMessage>
{
    LLMMessage.System("You are a helpful coding assistant"),
    LLMMessage.User("How do I sort a list in C#?")
};

var response = await _llm.ChatAsync(messages);
```

### Streaming Responses

```csharp
await foreach (var chunk in _llm.StreamAsync(prompt))
{
    Console.Write(chunk);
}
```

## Key Design Principles

1. **Dependency Injection**: `ILLMProvider` is injected via constructor
2. **Provider-Agnostic**: Same code works with any LLM provider
3. **Configuration**: Settings from `appsettings.json` or environment variables
4. **Resilience**: Built-in retry policies for transient failures
5. **Logging**: Structured logging for debugging

## Switching Providers

To switch providers, just change the configuration in `Program.cs`:

```csharp
// From Ollama:
builder.AddOllamaProvider();

// To OpenAI:
builder.AddOpenAIProvider(options => {
    options.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!;
});
```

**No code changes needed in your tools!** That's the power of the `ILLMProvider` abstraction.

## Next Steps

- Add your own custom MCP tools using `[McpTool]` attribute
- Experiment with different models and temperature settings
- Try streaming responses for better UX
- Add error handling for production use
- Implement conversation history for multi-turn chat

## Learn More

- [DotnetFastMCP Documentation](../../Documentation/)
- [Ollama Models](https://ollama.ai/library)
- [OpenAI API Reference](https://platform.openai.com/docs/api-reference)
- [Azure OpenAI Service](https://azure.microsoft.com/en-us/products/ai-services/openai-service)

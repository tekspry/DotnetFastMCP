# Auto DI Registration & Parameter Descriptions Guide

> **Introduced in DotnetFastMCP v2.1.0**

This guide covers two developer-experience improvements shipped together in v2.1:

1. **Automatic DI Registration** — tool classes with constructor injection work without any manual `Services.Add...()` call
2. **`[McpDescription]`** — annotate parameters so AI models understand exactly what each one expects

---

## 1. Automatic DI Registration

### The Problem (before v2.1)

When you used `builder.WithComponentsFrom(assembly)` to scan for tools, FastMCP registered the tool *methods* but not the *declaring class* in ASP.NET Core's DI container. If your tool class required constructor injection, you had to manually add:

```csharp
// ❌ Before v2.1 — manual registration was required or the server would crash at runtime
builder.Services.AddTransient<WeatherTool>();
builder.Services.AddTransient<CurrencyTool>();
// ... one line per tool class
```

If you forgot even one, the server started fine but crashed at runtime with:
```
System.Reflection.TargetException: Non-static method requires a target.
```

### The Fix (v2.1+)

`WithComponentsFrom` now **automatically registers** every non-static tool class as `Transient` in DI. You write your tool classes normally with constructor injection, and everything just works:

```csharp
// ✅ After v2.1 — no manual registration needed
var builder = McpServerBuilder.Create(mcpServer, args);
builder.WithComponentsFrom(Assembly.GetExecutingAssembly()); // ← handles DI automatically

var app = builder.Build();
await app.RunMcpAsync(args);
```

### Tool Class With Constructor Injection

```csharp
public class ProductSearchTool
{
    private readonly IProductRepository _repo;
    private readonly ILogger<ProductSearchTool> _logger;

    // Constructor injection — resolved automatically from DI
    public ProductSearchTool(IProductRepository repo, ILogger<ProductSearchTool> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    [McpTool("search_products", Description = "Searches the product catalog")]
    public async Task<string> SearchAsync(string query, int limit = 10)
    {
        _logger.LogInformation("Searching for '{Query}'", query);
        var results = await _repo.SearchAsync(query, limit);
        return JsonSerializer.Serialize(results);
    }
}
```

No `builder.Services.AddTransient<ProductSearchTool>()` needed. Register only your dependencies:

```csharp
builder.Services.AddScoped<IProductRepository, SqlProductRepository>();
// ProductSearchTool itself is registered automatically
```

### Behaviour Details

| Scenario | Behaviour |
|---|---|
| Non-static tool class | ✅ Automatically registered as `Transient` |
| Static tool class | ✅ Not registered (static methods need no instance) |
| Class already registered via `AddScoped<T>()` | ✅ `TryAdd` semantics — existing registration wins |
| Class registered via `AddHttpClient<T>()` | ✅ `TryAdd` semantics — HTTP client registration preserved |
| Abstract class | ✅ Skipped silently |
| Top-level statement compiler types | ✅ Skipped silently |

### Using `AddHttpClient<T>` With Tools

If your tool class is a typed HTTP client, register it with `AddHttpClient<T>()` as normal — `WithComponentsFrom` will not override it:

```csharp
builder.Services.AddHttpClient<VisionTool>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
});
builder.WithComponentsFrom(Assembly.GetExecutingAssembly());
// VisionTool's HttpClient registration is preserved; Transient not added as duplicate
```

---

## 2. The `[McpDescription]` Attribute

### The Problem (before v2.1)

`tools/list` returned empty `description` for all parameters:

```json
{
  "name": "search_products",
  "inputSchema": {
    "type": "object",
    "properties": {
      "query":  { "type": "string", "description": "" },
      "limit":  { "type": "integer", "description": "" }
    }
  }
}
```

AI models had to guess what each parameter meant from the name alone. This caused incorrect calls or required you to document everything inside the tool-level `Description`.

### The Fix (v2.1+)

Apply `[McpDescription]` to individual parameters. The description is automatically included in the JSON Schema returned by `tools/list`:

```csharp
[McpTool("search_products", Description = "Searches the product catalog")]
public async Task<string> SearchAsync(
    [McpDescription("The search query string, e.g. 'red cotton saree'")] string query,
    [McpDescription("Maximum number of results (1–100, default 10)")] int limit = 10)
{
    ...
}
```

This produces:

```json
{
  "name": "search_products",
  "inputSchema": {
    "type": "object",
    "properties": {
      "query": { "type": "string", "description": "The search query string, e.g. 'red cotton saree'" },
      "limit": { "type": "integer", "description": "Maximum number of results (1–100, default 10)" }
    },
    "required": ["query"]
  }
}
```

### Usage Tips

- Keep descriptions **concise and specific** — one or two sentences at most
- Include **example values** where the format isn't obvious (`e.g. 'image/png'`)
- For optional parameters, mention the **default value** in the description
- Use **units and ranges** for numeric parameters (`1–100`, `seconds`, `pixels`)

### Framework-Injected Parameters Are Automatically Excluded

Framework parameters (`McpContext`, `CancellationToken`, `ClaimsPrincipal`, `IMcpSession`) are now correctly excluded from the schema in all cases. They never appear in `tools/list` output:

```csharp
[McpTool("process_image")]
public async Task<string> ProcessAsync(
    [McpDescription("The image URL or file path")] string imageUri, // ← appears in schema
    McpContext context,          // ← excluded automatically
    CancellationToken ct)        // ← excluded automatically
{
    ...
}
```

---

## Migration From v2.0

**No breaking changes.** Both improvements are entirely backward compatible:

- Existing static tool classes continue to work exactly as before
- Existing `builder.Services.AddTransient<T>()` calls remain valid and take precedence
- Parameters without `[McpDescription]` continue to work — they just have empty descriptions
- All existing tests pass without modification

To take advantage of the new features:
1. Upgrade to `DotnetFastMCP >= 2.1.0`
2. Remove any manual `builder.Services.AddTransient<MyTool>()` calls for tool classes (optional but recommended)
3. Add `[McpDescription("...")]` to your tool parameters for richer AI model integration

---

## See Also

- [`examples/BasicServer`](../examples/BasicServer/) — updated example demonstrating both features
- [`McpToolAttribute`](../src/FastMCP/Attributes/McpToolAttribute.cs) — tool-level description
- [`McpDescriptionAttribute`](../src/FastMCP/Attributes/McpDescriptionAttribute.cs) — parameter-level description

namespace FastMCP.Attributes;

/// <summary>
/// Provides a human-readable description for an MCP tool parameter.
/// This description is emitted into the JSON Schema returned by <c>tools/list</c>,
/// allowing AI models (Claude, Gemini, GPT-4, etc.) to understand what value
/// each parameter expects without relying on the parameter name alone.
/// </summary>
/// <example>
/// <code>
/// [McpTool("search_products", Description = "Searches the product catalog")]
/// public async Task&lt;string&gt; SearchAsync(
///     [McpDescription("The search query string, e.g. 'red cotton kurti'")] string query,
///     [McpDescription("Maximum number of results to return (1–100)")] int limit = 10)
/// { ... }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
public sealed class McpDescriptionAttribute : Attribute
{
    /// <summary>
    /// Gets the description text for the parameter.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Initialises a new instance of <see cref="McpDescriptionAttribute"/>.
    /// </summary>
    /// <param name="description">
    /// A concise, human-readable description of what the parameter represents
    /// and what values are valid. Shown to the AI model in the tool schema.
    /// </param>
    public McpDescriptionAttribute(string description)
    {
        Description = description;
    }
}

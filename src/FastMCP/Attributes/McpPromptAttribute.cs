namespace FastMCP.Attributes;
/// <summary>
/// Marks a method as an MCP prompt handler.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class McpPromptAttribute : Attribute
{
    public string? Name { get; }
    public string? Description { get; set; }
    public McpPromptAttribute(string? name = null)
    {
        Name = name;
    }
}
namespace FastMCP.Attributes;

/// <summary>
/// Marks a method as an MCP tool that can be executed by a model.
/// The method's name, parameters, and doc comment will be used to generate the tool's schema.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class McpToolAttribute : Attribute
{
    public string? Name { get; }
    public string? Description { get; set; }
    public string? Icon { get; set; }

    public McpToolAttribute(string? name = null)
    {
        Name = name;
    }
}

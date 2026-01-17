using System.Reflection;
using FastMCP.Protocol;

namespace FastMCP.Server;

/// <summary>
/// Represents a collection of MCP components (tools, resources) that define an MCP server.
/// This class is the central point for component registration.
/// </summary>
public class FastMCPServer
{
    public string Name { get; }
    public string Version { get; } = "1.0.0"; // Default version
    
    // Collections to store registered components
    public Dictionary<string, MethodInfo> Tools { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, MethodInfo> Resources { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, MethodInfo> Prompts { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<Tool> ToolsMetadata { get; } = new();
    public List<Resource> ResourcesMetadata { get; } = new();
    
    // Dictionary to store OpenAPI tool proxies or other dynamic tools by name.
    // This allows the middleware to look up tool handlers by name.
    public Dictionary<string, object> DynamicTools { get; } = new(StringComparer.OrdinalIgnoreCase);

    public FastMCPServer(string name)
    {
        Name = name;
    }

    public void Import(FastMCPServer other, string? prefix = null)
    {
        string p = string.IsNullOrEmpty(prefix) ? "" : $"{prefix}_";
        // 1. Import Tools
        foreach (var tool in other.Tools)
        {
            var newName = p + tool.Key;
            if (Tools.TryAdd(newName, tool.Value))
            {
                // Clone metadata with new name
                var existingMeta = other.ToolsMetadata.FirstOrDefault(m => m.Name == tool.Key);
                if (existingMeta != null)
                {
                    ToolsMetadata.Add(new Tool 
                    { 
                        Name = newName, 
                        Description = existingMeta.Description,
                        InputSchema = existingMeta.InputSchema
                    });
                }
            }
        }
        // 2. Import Resources
        foreach (var resource in other.Resources)
        {
            Resources.TryAdd(p + resource.Key, resource.Value);            
        }
        // 3. Import Prompts
        foreach (var prompt in other.Prompts)
        {
             Prompts.TryAdd(p + prompt.Key, prompt.Value);
        }
    }
}

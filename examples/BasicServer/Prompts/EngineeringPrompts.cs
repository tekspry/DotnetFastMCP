using FastMCP.Attributes;
using FastMCP.Protocol;

namespace BasicServer.Prompts;

/// <summary>
/// Prompts designed for software engineering tasks.
/// </summary>
public static class EngineeringPrompts
{
    /// <summary>
    /// Analyzes code snippets for potential issues and improvements.
    /// </summary>
    /// <param name="code">The code snippet to analyze.</param>
    /// <param name="language">The programming language of the snippet.</param>
    [McpPrompt("analyze_code", Description = "Analyzes a code snippet for potential improvements, bugs, or style issues.")]
    public static GetPromptResult Analyze(
        [McpDescription("The code snippet to analyze")] string code, 
        [McpDescription("The programming language of the snippet (e.g. csharp, python)")] string language = "csharp")
    {
        return new GetPromptResult
        {
            Description = "Analysis Result",
            Messages = new List<PromptMessage>
            {
                new PromptMessage 
                { 
                    Role = "user", 
                    Content = new TextContent
                    {
                         Text =  $"You are an expert static analysis tool. Please analyze this {language} code for bugs, performance issues, and style violations:\n\n{code}"
                    }
                }
            }
        };
    }

    /// <summary>
    /// Generates a unit test for a given function description.
    /// </summary>
    [McpPrompt("generate_test", Description = "Generates unit test code for a specified function and requirements.")]
    public static GetPromptResult GenerateTest(
        [McpDescription("The name of the function or method to test")] string functionName, 
        [McpDescription("The functional requirements and test scenarios to cover")] string requirements)
    {
        return new GetPromptResult
        {
            Description = "Generate Unit Test",
            Messages = new List<PromptMessage>
            {
                 new PromptMessage 
                { 
                    Role = "user", 
                    Content = new TextContent 
                    { 
                        Text = $"Write a comprehensive unit test using xUnit for a function named '{functionName}'.\nRequirements:\n{requirements}" 
                    }  
                }
            }
        };
    }
}

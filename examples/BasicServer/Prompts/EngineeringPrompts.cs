using FastMCP.Attributes;
using FastMCP.Protocol;
using System.Collections.Generic;

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
    [McpPrompt("analyze_code")]
    public static GetPromptResult Analyze(string code, string language = "csharp")
    {
        return new GetPromptResult
        {
            Description = "Analysis Result",
            Messages = new List<PromptMessage>
            {
                new PromptMessage 
                { 
                    Role = "user", 
                    Content = new { type = "text", text = $"You are an expert static analysis tool. Please analyze this {language} code for bugs, performance issues, and style violations:\n\n{code}" } 
                }
            }
        };
    }

    /// <summary>
    /// Generates a unit test for a given function description.
    /// </summary>
    [McpPrompt("generate_test")]
    public static GetPromptResult GenerateTest(string functionName, string requirements)
    {
        return new GetPromptResult
        {
            Description = "Generate Unit Test",
            Messages = new List<PromptMessage>
            {
                 new PromptMessage 
                { 
                    Role = "user", 
                    Content = new { type = "text", text = $"Write a comprehensive unit test using xUnit for a function named '{functionName}'.\nRequirements:\n{requirements}" } 
                }
            }
        };
    }
}

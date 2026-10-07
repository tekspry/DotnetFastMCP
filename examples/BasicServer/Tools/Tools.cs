using System;
using System.Globalization;
using System.Threading.Tasks;
using FastMCP;
using FastMCP.Attributes;
using Microsoft.Extensions.Logging;

namespace BasicServer.Tools;

// ─────────────────────────────────────────────────────────────────────────────
// Static tools (no DI needed) — arithmetic, text, and context utilities.
// Formatted to adhere to TDQS (Tool Definition Quality Score) standards.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// General tools for the MCP server.
/// All tools adhere to TDQS standards: snake_case verb_noun naming, explicit usage
/// boundaries with named alternatives, and behavioral transparency.
/// </summary>
public static class Tools
{
    /// <summary>
    /// Adds two integer numbers together.
    /// </summary>
    [McpTool("add_numbers", Description = "Calculates the arithmetic sum of two integers (a + b). Use this tool when you need to perform addition. Do not use for subtraction (use 'subtract_numbers' instead), multiplication (use 'multiply_numbers' instead), or division (use 'divide_numbers' instead). Deterministic, pure calculation with no side effects.")]
    public static int Add(
        [McpDescription("The first integer operand (addend). Range: -2,147,483,648 to 2,147,483,647.")] int a,
        [McpDescription("The second integer operand (addend) to add to 'a'. Range: -2,147,483,648 to 2,147,483,647.")] int b)
    {
        return a + b;
    }

    /// <summary>
    /// Subtracts the second integer from the first integer.
    /// </summary>
    [McpTool("subtract_numbers", Description = "Calculates the arithmetic difference between two integers (a - b). Use this tool when you need to subtract one value from another. Do not use for addition (use 'add_numbers' instead), multiplication (use 'multiply_numbers' instead), or division (use 'divide_numbers' instead). Deterministic pure calculation; returns negative values if the subtrahend 'b' is greater than minuend 'a'.")]
    public static int Subtract(
        [McpDescription("The minuend integer from which 'b' will be subtracted.")] int a,
        [McpDescription("The subtrahend integer to subtract from 'a'.")] int b)
    {
        return a - b;
    }

    /// <summary>
    /// Multiplies two integers.
    /// </summary>
    [McpTool("multiply_numbers", Description = "Calculates the arithmetic product of two integers (a * b). Use this tool when you need to multiply two numbers. Do not use for addition (use 'add_numbers' instead) or division (use 'divide_numbers' instead). Deterministic pure calculation with no side effects.")]
    public static int Multiply(
        [McpDescription("The first integer factor.")] int a,
        [McpDescription("The second integer factor to multiply with 'a'.")] int b)
    {
        return a * b;
    }

    /// <summary>
    /// Divides the first integer by the second integer.
    /// </summary>
    [McpTool("divide_numbers", Description = "Calculates the integer quotient of two numbers (a / b). Use this tool for integer floor division. Do not use for addition (use 'add_numbers' instead) or multiplication (use 'multiply_numbers' instead). Returns floor quotient; throws an error if divisor 'b' is zero.")]
    public static int Divide(
        [McpDescription("The dividend integer to divide.")] int a,
        [McpDescription("The divisor integer to divide by. Must not be zero.")] int b)
    {
        if (b == 0)
        {
            throw new ArgumentException("Divisor 'b' cannot be zero.", nameof(b));
        }
        return a / b;
    }

    /// <summary>
    /// Formats raw text casing or whitespace.
    /// </summary>
    [McpTool("format_text", Description = "Transforms and normalizes string casing and whitespace. Use this tool for text standardization like uppercase, lowercase, titlecase, or trimming. Do not use for generating interpersonal greetings (use 'greet_user' instead). Deterministic in-memory string transformation.")]
    public static string FormatText(
        [McpDescription("The raw text string to format.")] string text,
        [McpDescription("The formatting operation: 'uppercase', 'lowercase', 'titlecase', or 'trim'. Defaults to 'trim'.")] string operation = "trim")
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return operation.ToLowerInvariant() switch
        {
            "uppercase" => text.ToUpperInvariant(),
            "lowercase" => text.ToLowerInvariant(),
            "titlecase" => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text),
            _ => text.Trim()
        };
    }

    /// <summary>
    /// Processes task input with progress reporting and contextual logging via McpContext.
    /// </summary>
    [McpTool("process_task", Description = "Executes a simulated task with progress tracking and context logging. Use this tool when executing operations that require live progress updates and diagnostic log streaming via McpContext. Do not use for instant math calculations. Emits real-time progress notifications at 10% and 100% completion milestones via JSON-RPC.")]
    public static async Task<string> ProcessTask(
        [McpDescription("The task description or input payload string to process.")] string input,
        McpContext context)   // McpContext is framework-injected — excluded from schema automatically
    {
        await context.LogInfoAsync($"Received task input: {input}");
        await context.ReportProgressAsync(10, 100);
        await Task.Delay(100);
        await context.ReportProgressAsync(100, 100);
        return $"Processed: {input}";
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Instance tool with constructor injection — demonstrates v2.1 auto-DI feature.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// A non-static tool that uses constructor injection.
/// With DotnetFastMCP v2.1+, no manual DI registration is required —
/// <c>builder.WithComponentsFrom(Assembly.GetExecutingAssembly())</c>
/// automatically registers this class as Transient.
/// </summary>
public class GreetingTool
{
    private readonly ILogger<GreetingTool> _logger;

    public GreetingTool(ILogger<GreetingTool> logger)
    {
        _logger = logger;
    }

    [McpTool("greet_user", Description = "Generates a personalized greeting message for a user. Use this tool to produce customized salutations in formal or casual tone when interacting with humans. Do not use for raw text formatting or casing changes (use 'format_text' instead). Safe, idempotent formatting.")]
    public string Greet(
        [McpDescription("The name of the user to greet (e.g. 'Alice'). Must not be empty.")] string name,
        [McpDescription("The greeting style tone: 'formal' for professional salutations or 'casual' for friendly greetings. Defaults to 'casual'.")] string style = "casual")
    {
        _logger.LogInformation("Greeting user '{Name}' with style '{Style}'", name, style);

        return style.Equals("formal", StringComparison.OrdinalIgnoreCase)
            ? $"Good day, {name}. How may I assist you today?"
            : $"Hey {name}! What can I do for you?";
    }
}

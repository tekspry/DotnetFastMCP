namespace FastMCP.AI;

/// <summary>
/// Represents a message in a chat conversation.
/// </summary>
public record LLMMessage(string Role, string Content)
{
    /// <summary>
    /// Creates a system message.
    /// </summary>
    public static LLMMessage System(string content) => new("system", content);

    /// <summary>
    /// Creates a user message.
    /// </summary>
    public static LLMMessage User(string content) => new("user", content);

    /// <summary>
    /// Creates an assistant message.
    /// </summary>
    public static LLMMessage Assistant(string content) => new("assistant", content);
}

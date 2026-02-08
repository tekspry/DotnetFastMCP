using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FastMCP.AI;

/// <summary>
/// Abstraction for LLM providers that can generate text completions.
/// </summary>
public interface ILLMProvider
{
    /// <summary>
    /// Generates a text completion for the given prompt.
    /// </summary>
    /// <param name="prompt">The input prompt text</param>
    /// <param name="options">Optional generation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The generated text response</returns>
    Task<string> GenerateAsync(
        string prompt, 
        LLMGenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a streaming text completion for the given prompt.
    /// </summary>
    /// <param name="prompt">The input prompt text</param>
    /// <param name="options">Optional generation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>An async enumerable of text chunks</returns>
    IAsyncEnumerable<string> StreamAsync(
        string prompt,
        LLMGenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a chat completion for the given messages.
    /// </summary>
    /// <param name="messages">The conversation history</param>
    /// <param name="options">Optional generation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The assistant's response message</returns>
    Task<string> ChatAsync(
        IEnumerable<LLMMessage> messages,
        LLMGenerationOptions? options = null,
        CancellationToken cancellationToken = default);
}

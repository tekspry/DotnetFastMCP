using System;
using System.Threading;
using System.Threading.Tasks;
namespace FastMCP.Background;
/// <summary>
/// Interface for a queue of background work items.
/// </summary>
public interface IBackgroundTaskQueue
{
    /// <summary>
    /// Queues a work item to be executed in the background.
    /// </summary>
    /// <param name="workItem">A function that accepts a cancellation token and returns a ValueTask.</param>
    ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem);
    /// <summary>
    /// Dequeues a work item.
    /// </summary>
    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
}
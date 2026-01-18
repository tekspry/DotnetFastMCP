using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
namespace FastMCP.Background;
public class McpBackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<CancellationToken, ValueTask>> _queue;
    public McpBackgroundTaskQueue()
    {
        // Unbounded by default, but could be configured via options
        var options = new UnboundedChannelOptions
        {
            SingleReader = true, // We will have one background service reading
            SingleWriter = false // Multiple tools can write
        };
        _queue = Channel.CreateUnbounded<Func<CancellationToken, ValueTask>>(options);
    }
    public async ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem)
    {
        if (workItem == null)
        {
            throw new ArgumentNullException(nameof(workItem));
        }
        await _queue.Writer.WriteAsync(workItem);
    }
    public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
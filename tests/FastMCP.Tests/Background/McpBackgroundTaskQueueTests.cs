using FastMCP.Background;
using Xunit;

namespace FastMCP.Tests.Background;

public class McpBackgroundTaskQueueTests
{
    [Fact]
    public async Task QueueAndDequeue_WorkItemExecutes()
    {
        var queue = new McpBackgroundTaskQueue();
        bool executed = false;

        await queue.QueueBackgroundWorkItemAsync(ct =>
        {
            executed = true;
            return ValueTask.CompletedTask;
        });

        var workItem = await queue.DequeueAsync(CancellationToken.None);
        await workItem(CancellationToken.None);

        Assert.True(executed);
    }

    [Fact]
    public async Task QueueNullItem_ThrowsArgumentNullException()
    {
        var queue = new McpBackgroundTaskQueue();
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await queue.QueueBackgroundWorkItemAsync(null!);
        });
    }
}

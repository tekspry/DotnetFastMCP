using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace FastMCP.Background;
public class McpBackgroundService : BackgroundService
{
    private readonly ILogger<McpBackgroundService> _logger;
    private readonly IBackgroundTaskQueue _taskQueue;
    public McpBackgroundService(IBackgroundTaskQueue taskQueue, ILogger<McpBackgroundService> logger)
    {
        _taskQueue = taskQueue;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MCP Background Task Service starting.");
        while (!stoppingToken.IsCancellationRequested)
        {
            Func<CancellationToken, ValueTask>? workItem = null;
            try
            {
                // Wait for a task to appear in the queue
                workItem = await _taskQueue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if stoppingToken was signaled
                break;
            }
            try
            {
                // Execute the task
                await workItem(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing background work item.");
            }
        }
        
        _logger.LogInformation("MCP Background Task Service stopping.");
    }
}
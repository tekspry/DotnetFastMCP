using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
namespace FastMCP.Hosting;
public static class McpHostingExtensions
{
    public static async Task RunMcpAsync(this WebApplication app, string[] args)
    {
        if (args.Contains("--stdio"))
        {
            var transport = app.Services.GetRequiredService<McpStdioTransport>();
            
            // CRITICAL: We must manually start IHostedServices (like BackgroundService)
            // because we are NOT calling app.RunAsync() which usually does this.
            var hostedServices = app.Services.GetServices<IHostedService>();
            foreach (var service in hostedServices)
            {
                await service.StartAsync(CancellationToken.None);
            }

            try 
            {
                await transport.RunAsync();
            }
            finally
            {
                // Graceful shutdown
                foreach (var service in hostedServices.Reverse())
                {
                    await service.StopAsync(CancellationToken.None);
                }
            }
        }
        else
        {
            await app.RunAsync();
        }
    }
}
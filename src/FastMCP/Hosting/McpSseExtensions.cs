using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace FastMCP.Hosting;

public static class McpSseExtensions
{
    public static IServiceCollection AddMcpSse(this IServiceCollection services)
    {
        services.AddSingleton<McpSseSessionManager>();
        return services;
    }

    public static IApplicationBuilder UseMcpSse(this IApplicationBuilder app)
    {
        return app.UseMiddleware<McpSseMiddleware>();
    }
}
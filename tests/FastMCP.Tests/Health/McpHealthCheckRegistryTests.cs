using FastMCP.Health;
using FastMCP.Server;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FastMCP.Tests.Health;

public class McpHealthCheckRegistryTests
{
    private static McpHealthCheckRegistry CreateRegistry(McpHealthCheckOptions options)
    {
        var server = new FastMCPServer("test-server");
        return new McpHealthCheckRegistry(options, server, NullLogger<McpHealthCheckRegistry>.Instance);
    }

    [Fact]
    public async Task NoChecks_ReturnsHealthy()
    {
        var registry = CreateRegistry(new McpHealthCheckOptions());
        var result = await registry.RunAllChecksAsync();

        Assert.Equal("Healthy", result.Status);
        // The built-in mcp_server check is always present
        Assert.Single(result.Checks);
        Assert.Equal("mcp_server", result.Checks[0].Name);
    }

    [Fact]
    public async Task AllChecksPass_ReturnsHealthy()
    {
        var options = new McpHealthCheckOptions()
            .AddCheck("check_a", () => true)
            .AddCheck("check_b", () => true);

        var registry = CreateRegistry(options);
        var result = await registry.RunAllChecksAsync();

        Assert.Equal("Healthy", result.Status);
        Assert.Equal(3, result.Checks.Count); // mcp_server + 2 custom
    }

    [Fact]
    public async Task OneCheckFails_ReturnsUnhealthy()
    {
        var options = new McpHealthCheckOptions()
            .AddCheck("always_fail", () => false);

        var registry = CreateRegistry(options);
        var result = await registry.RunAllChecksAsync();

        Assert.Equal("Unhealthy", result.Status);
        Assert.Contains(result.Checks, c => c.Name == "always_fail" && c.Status == "Unhealthy");
    }

    [Fact]
    public async Task CheckTimesOut_ReturnsDegraded()
    {
        var options = new McpHealthCheckOptions { MaxResponseTimeMs = 50 };
        options.AddAsyncCheck("slow_check", async ct =>
        {
            await Task.Delay(5_000, ct); // will time out
            return true;
        });

        var registry = CreateRegistry(options);
        var result = await registry.RunAllChecksAsync();

        Assert.Equal("Degraded", result.Status);
        var entry = Assert.Single(result.Checks, c => c.Name == "slow_check");
        Assert.Equal("Degraded", entry.Status);
    }

    [Fact]
    public async Task CheckThrows_ReturnsUnhealthy_WithMessage()
    {
        var options = new McpHealthCheckOptions();
        options.AddAsyncCheck("throwing_check", _ => throw new InvalidOperationException("db down"));

        var registry = CreateRegistry(options);
        var result = await registry.RunAllChecksAsync();

        Assert.Equal("Unhealthy", result.Status);
        var entry = Assert.Single(result.Checks, c => c.Name == "throwing_check");
        Assert.Equal("Unhealthy", entry.Status);
        Assert.Equal("db down", entry.Description);
    }

    [Fact]
    public async Task DiagnosticsIncluded_WhenEnabled()
    {
        var options = new McpHealthCheckOptions { IncludeServerDiagnostics = true };
        var registry = CreateRegistry(options);
        var result = await registry.RunAllChecksAsync();

        Assert.NotNull(result.Diagnostics);
        Assert.Equal("test-server", result.Diagnostics!.ServerName);
    }

    [Fact]
    public async Task DiagnosticsNotIncluded_WhenDisabled()
    {
        var options = new McpHealthCheckOptions { IncludeServerDiagnostics = false };
        var registry = CreateRegistry(options);
        var result = await registry.RunAllChecksAsync();

        Assert.Null(result.Diagnostics);
    }
}

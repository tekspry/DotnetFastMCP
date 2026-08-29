using FastMCP.Storage;
using Xunit;

namespace FastMCP.Tests.Storage;

public class McpStorageTests
{
    [Fact]
    public async Task InMemoryMcpStorage_SetAndGet_ReturnsStoredValue()
    {
        var storage = new InMemoryMcpStorage();
        await storage.SetAsync("test_key", new { Name = "FastMCP", Version = 2 });

        var result = await storage.GetAsync<TestStorageModel>("test_key");

        Assert.NotNull(result);
        Assert.Equal("FastMCP", result!.Name);
        Assert.Equal(2, result.Version);
    }

    [Fact]
    public async Task InMemoryMcpStorage_GetNonExistentKey_ReturnsNull()
    {
        var storage = new InMemoryMcpStorage();
        var result = await storage.GetAsync<TestStorageModel>("non_existent_key");

        Assert.Null(result);
    }

    [Fact]
    public async Task InMemoryMcpStorage_DeleteKey_RemovesValue()
    {
        var storage = new InMemoryMcpStorage();
        await storage.SetAsync("to_delete", "value123");

        var beforeDelete = await storage.GetAsync<string>("to_delete");
        Assert.Equal("value123", beforeDelete);

        await storage.DeleteAsync("to_delete");
        var afterDelete = await storage.GetAsync<string>("to_delete");
        Assert.Null(afterDelete);
    }
}

public class TestStorageModel
{
    public string Name { get; set; } = "";
    public int Version { get; set; }
}

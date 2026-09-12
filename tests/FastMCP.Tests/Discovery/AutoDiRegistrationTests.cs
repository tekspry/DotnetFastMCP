using System.Reflection;
using FastMCP.Attributes;
using FastMCP.Hosting;
using FastMCP.Server;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FastMCP.Tests.Discovery;

// ── Test fixtures ────────────────────────────────────────────────────────────

public class NonStaticToolClass
{
    [McpTool("instance_tool", Description = "An instance tool")]
    public string InstanceTool(
        [McpDescription("Some input value")] string input) => input;
}

public class AnotherNonStaticToolClass
{
    [McpTool("another_tool")]
    public string AnotherTool(string value) => value;
}

public static class StaticToolClass
{
    [McpTool("static_tool")]
    public static string StaticTool(string input) => input;
}

public abstract class AbstractToolClass
{
    [McpTool("abstract_tool")]
    public abstract string AbstractTool(string input);
}

// ── Tests ────────────────────────────────────────────────────────────────────

public class AutoDiRegistrationTests
{
    /// <summary>
    /// The core scenario: a non-static tool class with no manual DI registration
    /// must be automatically registered by WithComponentsFrom.
    /// </summary>
    [Fact]
    public void WithComponentsFrom_NonStaticTool_AutoRegistersInDI()
    {
        var server = new FastMCPServer("test");
        var builder = McpServerBuilder.Create(server);

        // Only scan the assembly containing NonStaticToolClass
        builder.WithComponentsFrom(typeof(NonStaticToolClass).Assembly);

        // Build the service provider to inspect registrations
        var app = builder.Build();
        var resolved = app.Services.GetService<NonStaticToolClass>();

        Assert.NotNull(resolved);
    }

    /// <summary>
    /// Static tool methods have no instance, so their declaring type must NOT be
    /// registered. Registering it would be nonsensical since static classes can't
    /// be instantiated.
    /// </summary>
    [Fact]
    public void WithComponentsFrom_StaticTool_DoesNotRegisterDeclaringTypeInDI()
    {
        var server = new FastMCPServer("test");
        var builder = McpServerBuilder.Create(server);
        builder.WithComponentsFrom(typeof(StaticToolClass).Assembly);

        // Verify that StaticToolClass was NOT added to the service collection
        // (static classes can't be instantiated, so DI registration makes no sense)
        var isRegistered = builder.Services
            .Any(sd => sd.ServiceType == typeof(StaticToolClass));

        Assert.False(isRegistered,
            "Static tool class must not be registered in DI — static classes have no instance");
    }

    /// <summary>
    /// TryAddTransient semantics: if the developer has already registered the type
    /// (e.g. via AddHttpClient<T> or AddScoped<T>), WithComponentsFrom must not
    /// override that registration.
    /// </summary>
    [Fact]
    public void WithComponentsFrom_AlreadyRegisteredAsSingleton_IsNotOverridden()
    {
        var server = new FastMCPServer("test");
        var builder = McpServerBuilder.Create(server);

        // Explicitly register as Singleton BEFORE scanning
        builder.Services.AddSingleton<NonStaticToolClass>();
        builder.WithComponentsFrom(typeof(NonStaticToolClass).Assembly);

        var app = builder.Build();

        // Resolve twice — should get same instance (Singleton behaviour preserved)
        var scope1 = app.Services.CreateScope();
        var scope2 = app.Services.CreateScope();
        var instance1 = scope1.ServiceProvider.GetRequiredService<NonStaticToolClass>();
        var instance2 = scope2.ServiceProvider.GetRequiredService<NonStaticToolClass>();

        // If TryAdd didn't override the Singleton, both scopes return the same instance
        Assert.Same(instance1, instance2);
    }

    /// <summary>
    /// Abstract classes cannot be directly instantiated.
    /// The scanner must skip them silently.
    /// </summary>
    [Fact]
    public void WithComponentsFrom_AbstractType_IsSkipped()
    {
        var server = new FastMCPServer("test");
        var builder = McpServerBuilder.Create(server);
        builder.WithComponentsFrom(typeof(AbstractToolClass).Assembly);

        var app = builder.Build();
        // Should not throw; abstract type must not appear in registrations
        var resolved = app.Services.GetService<AbstractToolClass>();
        Assert.Null(resolved);
    }

    /// <summary>
    /// Multiple tool classes scanned from the same assembly are all registered.
    /// </summary>
    [Fact]
    public void WithComponentsFrom_MultipleNonStaticToolClasses_AllRegistered()
    {
        var server = new FastMCPServer("test");
        var builder = McpServerBuilder.Create(server);
        builder.WithComponentsFrom(typeof(NonStaticToolClass).Assembly);

        var app = builder.Build();

        var first = app.Services.GetService<NonStaticToolClass>();
        var second = app.Services.GetService<AnotherNonStaticToolClass>();

        Assert.NotNull(first);
        Assert.NotNull(second);
    }

    /// <summary>
    /// Tools are still registered in the server's tool dictionary alongside DI registration.
    /// Both concerns are handled correctly together.
    /// </summary>
    [Fact]
    public void WithComponentsFrom_NonStaticTool_ToolAlsoRegisteredInServerDictionary()
    {
        var server = new FastMCPServer("test");
        var builder = McpServerBuilder.Create(server);
        builder.WithComponentsFrom(typeof(NonStaticToolClass).Assembly);

        Assert.True(server.Tools.ContainsKey("instance_tool"));
    }
}

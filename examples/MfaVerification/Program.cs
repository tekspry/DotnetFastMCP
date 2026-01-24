using FastMCP;
using FastMCP.Attributes;
using FastMCP.Hosting;
using FastMCP.Server;
using FastMCP.Protocol;
using FastMCP.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Security.Claims;

Console.WriteLine("=== MFA Support Verification ===");

// 1. Setup Server & Tool
var server = new FastMCPServer("MfaTestServer");
var toolMethod = typeof(SensitiveTools).GetMethod(nameof(SensitiveTools.ProtectedTool));
server.Tools.TryAdd("protected_tool", toolMethod!);

// 2. Setup Handler Dependencies
var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton<IMcpStorage, InMemoryMcpStorage>();
services.AddSingleton<IAuthorizationService, AlwaysAllowAuthorizationService>(); // Mock Auth Service
var sp = services.BuildServiceProvider();

// 3. Create Handler
var handler = new McpRequestHandler(
    sp.GetRequiredService<IAuthorizationService>(),
    sp.GetRequiredService<IMcpStorage>(),
    sp
);

// 4. Run Tests

// Scenario A: No User
Console.WriteLine("\nTest A: No User (Should Fail)");
await RunTest(handler, server, null, expectedSuccess: false);

// Scenario B: User, No Claims
Console.WriteLine("\nTest B: User, No Claims (Should Fail)");
var userNoClaims = new ClaimsPrincipal(new ClaimsIdentity());
await RunTest(handler, server, userNoClaims, expectedSuccess: false);

// Scenario C: User, Wrong AMR
Console.WriteLine("\nTest C: User, Wrong AMR (pwd) (Should Fail)");
var userWrongAmr = new ClaimsPrincipal(new ClaimsIdentity(new[] 
{ 
    new Claim("sub", "user1"),
    new Claim("amr", "pwd") 
}, "TestAuth"));
await RunTest(handler, server, userWrongAmr, expectedSuccess: false);

// Scenario D: User, Correct MFA
Console.WriteLine("\nTest D: User, MFA Claim (Should Succeed)");
var userMfa = new ClaimsPrincipal(new ClaimsIdentity(new[] 
{ 
    new Claim("sub", "user1"),
    new Claim("amr", "mfa") 
}, "TestAuth"));
await RunTest(handler, server, userMfa, expectedSuccess: true);

Console.WriteLine("\n=== Verification Complete ===");

static async Task RunTest(McpRequestHandler handler, FastMCPServer server, ClaimsPrincipal? user, bool expectedSuccess)
{
    var request = new JsonRpcRequest
    {
        JsonRpc = "2.0",
        Id = 1,
        Method = "protected_tool",
        Params = new { }
    };

    var response = await handler.HandleRequestAsync(request, server, user);

    if (response.Error != null)
    {
        // Failed
        if (!expectedSuccess)
        {
            Console.WriteLine($"[PASS] Request denied as expected. Error: {response.Error.Message}");
        }
        else
        {
            Console.WriteLine($"[FAIL] Request denied but expected success. Error: {response.Error.Message}");
        }
    }
    else
    {
        // Succeeded
        if (expectedSuccess)
        {
            Console.WriteLine($"[PASS] Request succeeded as expected. Result: {response.Result}");
        }
        else
        {
            Console.WriteLine($"[FAIL] Request succeeded but expected failure.");
        }
    }
}

// --- Helper Classes ---

public static class SensitiveTools
{
    [McpTool("protected_tool")]
    [AuthorizeMcpTool(RequireMfa = true)]
    public static string ProtectedTool()
    {
        return "Access Granted!";
    }
}

public class AlwaysAllowAuthorizationService : IAuthorizationService
{
    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
    {
        return Task.FromResult(AuthorizationResult.Success());
    }

    public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
    {
        return Task.FromResult(AuthorizationResult.Success());
    }
}

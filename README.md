# DotnetFastMCP - Enterprise-Grade Model Context Protocol Server Framework

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![GitHub](https://img.shields.io/badge/GitHub-tekspry-black)](https://github.com/tekspry/.NetFastMCP)

**A modern, production-ready C#/.NET framework for building secure, scalable Model Context Protocol (MCP) servers with enterprise-grade authentication.**

## 🎯 Overview

DotnetFastMCP provides a clean, attribute-based approach to building MCP servers that implement the JSON-RPC 2.0 protocol. It includes a **native .NET Client Library** client for consuming MCP servers, making it a complete solution for building both sides of the Model Context Protocol. Built on ASP.NET Core, it leverages modern .NET features for high performance, reliability, and **comprehensive OAuth 2.0 / OpenID Connect authentication** out of the box.

### ⭐ Key Features

#### Core Framework
- ✅ **Simple Attribute-Based API** - Declare tools and resources with `[McpTool]` and `[McpResource]` attributes
- ✅ **First-Class Prompts Support** - Define prompts with `[McpPrompt]` for LLM interaction templates
- ✅ **Automatic Component Discovery** - Reflection-based scanning of assemblies
- ✅ **JSON-RPC 2.0 Compliant** - Full protocol compliance with proper error handling
- ✅ **Flexible Parameter Binding** - Supports both array and named parameters
- ✅ **Built on ASP.NET Core** - Leverage the powerful ASP.NET Core hosting model
- ✅ **Production Ready** - Comprehensive error handling and logging
- ✅ **Type-Safe** - Full C# type system integration

#### 🔐 Enterprise Authentication
- ✅ **6 OAuth Providers Supported** - Azure AD, Google, GitHub, Auth0, Okta, AWS Cognito
- ✅ **OAuth Proxy Built-In** - Automatic Dynamic Client Registration (DCR) for non-DCR providers
- ✅ **JWT Token Verification** - Automatic token validation with JWKS caching
- ✅ **Zero Configuration** - Set environment variables and go
- ✅ **Sensible Defaults** - Pre-configured scopes for common use cases
- ✅ **Fine-Grained Authorization** - Protect tools with `[Authorize]` attribute
- ✅ **Claims-Based Access** - Access user information from authenticated requests
- ✅ **MFA Support** - Enforce Multi-Factor Authentication for sensitive tools

#### 🔌 Native Client Library (NEW!)
- ✅ **McpClient** - Type-safe .NET client for consuming any MCP server
- ✅ **Transport Agnostic** - Support for both Stdio and SSE connections
- ✅ **Notification Handling** - Events for real-time logs and progress
- ✅ **Tool Invocation** - Clean `CallToolAsync<T>` API

## 🚀 Quick Start

### Installation

```bash
git clone https://github.com/tekspry/.NetFastMCP.git
cd DotnetFastMCP
dotnet build -c Release
```

### Create Your First MCP Server

#### 1. Define Your Tools
### Running the Example Server

```bash
cd examples/BasicServer
dotnet run
```

The server will start on `http://localhost:5000`.

## 📚 Architecture

### Core Components

```
DotnetFastMCP/
├── src/
│   ├── FastMCP/
│   │   ├── Attributes/          # Component declaration attributes
│   │   ├── Client/              # 🔌 Client library implementation
│   │   ├── Hosting/             # Server hosting and middleware
│   │   ├── Protocol/            # JSON-RPC protocol implementation
│   │   ├── Server/              # FastMCPServer core class
│   │   └── FastMCP.csproj
│   └── FastMCP.CLI/             # Command-line utilities
├── examples/
│   └── BasicServer/             # Example MCP server implementation
├── tests/
│   └── McpIntegrationTest/      # Integration tests
├── LAUNCH_TESTS.ps1             # PowerShell test suite launcher
└── RUN_AND_TEST.ps1             # PowerShell integration test script
```

### Project Structure

| Project | Purpose |
|---------|---------|
| `FastMCP` | Core framework library |
| `FastMCP.CLI` | Command-line interface tools |
| `BasicServer` | Example MCP server implementation |
| `McpIntegrationTest` | Integration tests |
| `ClientDemo` | Example Client consuming BasicServer |

## 🔧 Creating an MCP Server

### 1. Define Components

For better organization, split your components into multiple files (e.g., `Tools.cs`, `Resources.cs`). The framework will discover them automatically.

**File: `Tools.cs`**
```csharp
using FastMCP.Attributes;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

public static class MyTools
{
    /// <summary>
    /// Public tool - no authentication required
    /// </summary>
    [McpTool]
    public static int Add(int a, int b) => a + b;

public static class Resources
{
    /// <summary>
    /// Protected tool - requires authentication
    /// </summary>
    [McpTool]
    [Authorize]
    public static object GetUserProfile(ClaimsPrincipal user)
    {
        return new
        {
            Name = user.Identity?.Name,
            Email = user.FindFirst("email")?.Value,
            IsAuthenticated = user.Identity?.IsAuthenticated
        };
    }
}
```

#### 2. Configure Server with Authentication

```csharp
using FastMCP.Hosting;
using FastMCP.Server;
using System.Reflection;

var mcpServer = new FastMCPServer(name: "My Secure MCP Server");
var builder = McpServerBuilder.Create(mcpServer, args);

// Add authentication (choose your provider)
builder.AddAzureAdTokenVerifier();  // or AddGoogleTokenVerifier(), AddGitHubTokenVerifier(), etc.

// Register tools
builder.WithComponentsFrom(Assembly.GetExecutingAssembly());

var app = builder.Build();
app.Urls.Add("http://localhost:5002");
await app.RunAsync();
```

#### 3. Set Environment Variables

```powershell
# Windows PowerShell
$env:FASTMCP_SERVER_AUTH_AZUREAD_TENANT_ID="your-tenant-id"
$env:FASTMCP_SERVER_AUTH_AZUREAD_CLIENT_ID="your-client-id"
$env:FASTMCP_SERVER_AUTH_AZUREAD_CLIENT_SECRET="your-client-secret"
```

```bash
# Linux/Mac
export FASTMCP_SERVER_AUTH_AZUREAD_TENANT_ID="your-tenant-id"
export FASTMCP_SERVER_AUTH_AZUREAD_CLIENT_ID="your-client-id"
export FASTMCP_SERVER_AUTH_AZUREAD_CLIENT_SECRET="your-client-secret"
```

#### 4. Run and Test

```bash
dotnet run
```

Your server is now running with **OAuth Proxy** endpoints:
- MCP endpoint: `http://localhost:5002/mcp`
- OAuth authorization: `http://localhost:5002/oauth/authorize`
- OAuth token: `http://localhost:5002/oauth/token`
- Discovery: `http://localhost:5002/.well-known/oauth-authorization-server`

#### Stdio Mode
You can also run the server in Stdio mode (for local LLM clients):
```bash
dotnet run -- --stdio
```

### Create an MCP Client

Connect to any MCP server using the C# Client Library:

```csharp
using FastMCP.Client;
using FastMCP.Client.Transports;

// 1. Connect (via Stdio or SSE)
var transport = new StdioClientTransport("dotnet", "run --project examples/BasicServer -- --stdio");
await using var client = new McpClient(transport);
await client.ConnectAsync();

// 2. List & Call Tools
var tools = await client.ListToolsAsync();
var result = await client.CallToolAsync<int>("add_numbers", new { a = 10, b = 20 });
```

## 🔐 Authentication Providers

DotnetFastMCP supports **6 enterprise-grade OAuth providers** out of the box:

| Provider | Method | Use Case | Default Scopes |
|----------|--------|----------|----------------|
| **Azure AD** | `AddAzureAdTokenVerifier()` | Enterprise apps, Microsoft 365 | `openid`, `profile`, `email`, `offline_access` |
| **Google** | `AddGoogleTokenVerifier()` | Consumer apps, Google Workspace | `openid`, `profile`, `email`, `userinfo.profile` |
| **GitHub** | `AddGitHubTokenVerifier()` | Developer tools, repositories | `read:user`, `user:email` |
| **Auth0** | `AddAuth0TokenVerifier()` | Multi-tenant SaaS, custom identity | `openid`, `profile`, `email`, `offline_access` |
| **Okta** | `AddOktaTokenVerifier()` | Enterprise SSO, workforce identity | `openid`, `profile`, `email`, `offline_access` |
| **AWS Cognito** | `AddAwsCognitoTokenVerifier()` | AWS-native apps, user pools | `openid`, `profile`, `email` |

### Quick Setup Examples

<details>
<summary><b>Azure AD</b></summary>

```csharp
builder.AddAzureAdTokenVerifier();
```

**Environment Variables:**
```bash
FASTMCP_SERVER_AUTH_AZUREAD_TENANT_ID=your-tenant-id
FASTMCP_SERVER_AUTH_AZUREAD_CLIENT_ID=your-client-id
FASTMCP_SERVER_AUTH_AZUREAD_CLIENT_SECRET=your-client-secret
```

**Example:** [`examples/Auth/AzureAdOAuth`](examples/Auth/AzureAdOAuth)
</details>

<details>
<summary><b>Google</b></summary>

```csharp
builder.AddGoogleTokenVerifier();
```

**Environment Variables:**
```bash
FASTMCP_SERVER_AUTH_GOOGLE_CLIENT_ID=your-client-id.apps.googleusercontent.com
FASTMCP_SERVER_AUTH_GOOGLE_CLIENT_SECRET=your-client-secret
```

**Example:** [`examples/Auth/GoogleOAuth`](examples/Auth/GoogleOAuth)
</details>

<details>
<summary><b>GitHub</b></summary>
### Run Unit & Integration Tests

```csharp
builder.AddGitHubTokenVerifier();
```

**Environment Variables:**
```bash
FASTMCP_SERVER_AUTH_GITHUB_CLIENT_ID=your-github-client-id
FASTMCP_SERVER_AUTH_GITHUB_CLIENT_SECRET=your-github-client-secret
```

**Example:** [`examples/Auth/GitHubOAuth`](examples/Auth/GitHubOAuth)
</details>
### PowerShell Integration Test Suite

The project includes a comprehensive PowerShell-based integration test suite that validates a running server end-to-end.

1.  **Publish the server** (from the root of the `DotnetFastMCP` project):
    ```sh
    dotnet publish -c Release -o ..\publish examples\BasicServer
    ```

2.  **Run the tests**:
    Open a PowerShell terminal and run the launcher script from the project root:
    ```powershell
    .\LAUNCH_TESTS.ps1
    ```
This will open a new window, start the `BasicServer`, and run a series of tests covering all tools and resources, including error handling.

### Example Manual Test

<details>
<summary><b>Auth0</b></summary>

```csharp
builder.AddAuth0TokenVerifier();
```

**Environment Variables:**
```bash
FASTMCP_SERVER_AUTH_AUTH0_DOMAIN=your-tenant.auth0.com
FASTMCP_SERVER_AUTH_AUTH0_AUDIENCE=https://your-api-identifier
FASTMCP_SERVER_AUTH_AUTH0_CLIENT_ID=your-client-id
FASTMCP_SERVER_AUTH_AUTH0_CLIENT_SECRET=your-client-secret
```

**Example:** [`examples/Auth/Auth0OAuth`](examples/Auth/Auth0OAuth)
</details>

<details>
<summary><b>Okta</b></summary>

```csharp
builder.AddOktaTokenVerifier();
```

**Environment Variables:**
```bash
FASTMCP_SERVER_AUTH_OKTA_DOMAIN=dev-123456.okta.com
FASTMCP_SERVER_AUTH_OKTA_AUDIENCE=api://default
FASTMCP_SERVER_AUTH_OKTA_CLIENT_ID=your-client-id
FASTMCP_SERVER_AUTH_OKTA_CLIENT_SECRET=your-client-secret
```

**Example:** [`examples/Auth/OktaOAuth`](examples/Auth/OktaOAuth)
</details>

<details>
<summary><b>AWS Cognito</b></summary>

```csharp
builder.AddAwsCognitoTokenVerifier();
```

**Environment Variables:**
```bash
FASTMCP_SERVER_AUTH_AWSCOGNITO_USER_POOL_ID=us-east-1_XXXXXXXXX
FASTMCP_SERVER_AUTH_AWSCOGNITO_REGION=us-east-1
FASTMCP_SERVER_AUTH_AWSCOGNITO_CLIENT_ID=your-app-client-id
FASTMCP_SERVER_AUTH_AWSCOGNITO_CLIENT_SECRET=your-app-client-secret
FASTMCP_SERVER_AUTH_AWSCOGNITO_DOMAIN=myapp.auth.us-east-1.amazoncognito.com
```

**Example:** [`examples/Auth/AwsCognitoOAuth`](examples/Auth/AwsCognitoOAuth)
</details>

## 📚 Architecture

### Project Structure

```
DotnetFastMCP/
├── src/
│   └── FastMCP/
│       ├── Attributes/              # Component declaration attributes
│       ├── Authentication/          # 🔐 OAuth providers & token verification
│       │   ├── Providers/          # Azure AD, Google, GitHub, Auth0, Okta, AWS
│       │   ├── Proxy/              # OAuth Proxy for DCR
│       │   └── Verification/       # JWT token validation
│       ├── Hosting/                 # Server hosting and middleware
│       ├── Protocol/                # JSON-RPC protocol implementation
│       └── Server/                  # FastMCPServer core class
├── examples/
│   ├── BasicServer/                 # Simple MCP server
│   └── Auth/                        # 🔐 Authentication examples
│       ├── AzureAdOAuth/           # Azure AD example
│       ├── GoogleOAuth/            # Google OAuth example
│       ├── GitHubOAuth/            # GitHub OAuth example
│       ├── Auth0OAuth/             # Auth0 example
│       ├── OktaOAuth/              # Okta example
│       └── AwsCognitoOAuth/        # AWS Cognito example
└── tests/
    └── McpIntegrationTest/          # Integration tests
```

### Project Structure (Client)
The `FastMCP` framework now includes a complete client implementation in `src/FastMCP/Client`.

```mermaid
graph TD
    App[Your App] -->|Uses| Client[McpClient]
    Client -->|IClientTransport| Trans[Transport Layer]
    Trans -->|Stdio| Local[Local Process]
    Trans -->|SSE/HTTP| Remote[Remote Server]
```

### Authentication Flow

```mermaid
sequenceDiagram
    participant Client
    participant MCP Server
    participant OAuth Provider
    
    Client->>MCP Server: Request with Bearer Token
    MCP Server->>Token Verifier: Validate Token
    Token Verifier->>OAuth Provider: Fetch JWKS (if needed)
    OAuth Provider-->>Token Verifier: Public Keys
    Token Verifier-->>MCP Server: Validated Claims
    MCP Server-->>Client: Protected Resource
```

## 🔧 Creating an MCP Server

### Basic Server (No Authentication)

```csharp
using FastMCP.Hosting;
using FastMCP.Server;
using System.Reflection;

var mcpServer = new FastMCPServer(name: "My MCP Server");
var builder = McpServerBuilder.Create(mcpServer, args);

builder.WithComponentsFrom(Assembly.GetExecutingAssembly());

var app = builder.Build();
await app.RunAsync();
```

### Secure Server (With Authentication)

```csharp
using FastMCP.Hosting;
using FastMCP.Server;
using System.Reflection;

var mcpServer = new FastMCPServer(name: "My Secure MCP Server");
var builder = McpServerBuilder.Create(mcpServer, args);

// Add authentication - automatically configures OAuth Proxy
builder.AddAzureAdTokenVerifier();  // or any other provider

builder.WithComponentsFrom(Assembly.GetExecutingAssembly());
Returns server metadata. The example server returns:

var app = builder.Build();
app.Urls.Add("http://localhost:5002");
await app.RunAsync();
```

### Protected Tools

```csharp
using FastMCP.Attributes;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

public static class SecureTools
{
    /// <summary>
    /// Public tool - anyone can call
    /// </summary>
    [McpTool]
    public static string Echo(string message) => message;

    /// <summary>
    /// Protected tool - requires valid OAuth token
    /// </summary>
    [McpTool]
    [Authorize]
    public static object GetUserInfo(ClaimsPrincipal user)
    {
        return new
        {
            Name = user.Identity?.Name ?? "Unknown",
            Email = user.FindFirst("email")?.Value ?? "Not available",
            IsAuthenticated = user.Identity?.IsAuthenticated ?? false,
            Claims = user.Claims.Select(c => new { c.Type, c.Value }).ToList()
        };
    }

    /// <summary>
    /// Role-based authorization
    /// </summary>
    [McpTool]
    [Authorize(Roles = "Admin")]
    public static string AdminOnly() => "Admin access granted";
}
```

## 📡 JSON-RPC Protocol

### Prompts

Prompts allow servers to provide templates that LLMs can use.

```csharp
using FastMCP.Attributes;
using FastMCP.Protocol;

public static class MyPrompts
{
    [McpPrompt("analyze_code")]
    public static GetPromptResult Analyze(string code)
    {
        return new GetPromptResult
        {
            Description = "Analyze the given code",
            Messages = new List<PromptMessage>
            {
                new PromptMessage 
                { 
                    Role = "user", 
                    Content = new { type = "text", text = $"Please analyze this code:\n{code}" } 
                }
            }
        };
    }
}
```

### Calling Tools

**Public Tool (No Auth):**
```json
POST /mcp
{
  "jsonrpc": "2.0",
  "method": "Echo",
  "params": ["Hello World"],
  "id": 1
}
```

**Protected Tool (With Auth):**
```json
POST /mcp
Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGc...

{
  "jsonrpc": "2.0",
  "method": "GetUserInfo",
  "params": [],
  "id": 2
}
```

## 🧪 Testing

### Run All Tests

```bash
dotnet test
```

### Test Authentication Flow

Each authentication example includes a comprehensive `.rest` file for testing:

```bash
# Open in VS Code with REST Client extension
code examples/Auth/AzureAdOAuth/azure-ad-auth-tests.rest
```

Test files include:
- ✅ Discovery endpoints
- ✅ Public tool tests
- ✅ Protected tool tests (should fail without auth)
- ✅ OAuth authorization flow
- ✅ Token exchange
- ✅ Provider-specific API calls

## 📖 Documentation

### Complete Authentication Guide

See [`mcp-authentication-guide.md`](mcp-authentication-guide.md) for:
- Detailed provider setup instructions
- OAuth flow walkthrough
- Troubleshooting guide
- Best practices
- Production deployment checklist

### Example Projects

| Example | Description | Port |
|---------|-------------|------|
| [BasicServer](examples/BasicServer) | Simple MCP server without authentication | 5000 |
| [AzureAdOAuth](examples/Auth/AzureAdOAuth) | Azure AD authentication example | 5002 |
| [GoogleOAuth](examples/Auth/GoogleOAuth) | Google OAuth example | 5000 |
| [GitHubOAuth](examples/Auth/GitHubOAuth) | GitHub OAuth example | 5001 |
| [Auth0OAuth](examples/Auth/Auth0OAuth) | Auth0 authentication example | 5005 |
| [OktaOAuth](examples/Auth/OktaOAuth) | Okta authentication example | 5007 |
| [AwsCognitoOAuth](examples/Auth/AwsCognitoOAuth) | AWS Cognito example | 5006 |


## 🏗️ Advanced Features

### Middleware Interception (NEW!)

Middleware allows you to intercept and modify JSON-RPC messages (requests and responses) flowing through the server pipeline. This is useful for logging, validation, modification, or custom monitoring.

1.  **Define Middleware:** Implement `IMcpMiddleware`.
2.  **Register Middleware:** Use `builder.AddMcpMiddleware<T>()`.

```csharp
public class LoggingMiddleware : IMcpMiddleware
{
    public async Task<JsonRpcResponse> InvokeAsync(McpMiddlewareContext context, McpMiddlewareDelegate next, CancellationToken ct)
    {
        Console.Error.WriteLine($"[LOG] Incoming: {context.Request.Method}");
        
        // Pass to next handler
        var response = await next(context, ct);
        
        Console.Error.WriteLine($"[LOG] Completed. Error: {response.Error != null}");
        return response;
    }
}

// In Program.cs:
builder.AddMcpMiddleware<LoggingMiddleware>();
```

### Server Composition (NEW!)

Mount other MCP servers into your main server instantiation. This supports a "Micro-MCP" architecture where you can compose a robust agent from smaller, focused modules.

```csharp
// 1. Create Sub-Server (e.g. GitHub Tools)
var githubServer = new FastMCPServer("GitHub");
// ... register tools ...

// 2. Import into Main Server with "gh" prefix
builder.AddServer(githubServer, prefix: "gh");

// Result:
// The client sees tools named: "gh_create_issue", "gh_get_repo", etc.
```

### MFA Support (NEW!)

Enforce Multi-Factor Authentication for sensitive tools.

```csharp
[McpTool("transfer_funds")]
[AuthorizeMcpTool(RequireMfa = true)]
public static string TransferFunds()
{
    return "Transferred!";
}
```

-   **MFA Check**: Verifies `amr` claim contains `mfa`.
-   **Security**: Provides granular protection for critical operations.

### Storage Abstraction
s (NEW!)

FastMCP now includes a built-in state persistence layer. Tools can request `McpContext` to access `IMcpStorage`.

```csharp
[McpTool]
public static async Task<string> SetValue(string key, string value, McpContext context)
{
    await context.Storage.SetAsync(key, value);
    return "Saved!";
}
```

The default implementation is **In-Memory**, but you can swap it for Redis, SQL, or File storage:

```csharp
builder.AddMcpStorage<MyRedisStorage>();
```

### Background Tasks (NEW!)

FastMCP allows tools to fire-and-forget long running operations using `RunInBackground`.

```csharp
[McpTool]
public static async Task<string> ProcessFile(string file, McpContext context)
{
    await context.RunInBackground(async (ct) => 
    {
        // This runs without blocking the client
        await HeavyProcessing(file, ct);
    });

    return "Processing started!";
}
```

### Icons Support (NEW!)

Enhance the user interface of clients by providing icons for your server and tools.

```csharp
// Server Icon
server.Icon = "https://myserver.com/logo.png";

// Tool Icon
[McpTool(Icon = "https://myserver.com/tools/calc.png")]
public static int Add(int a, int b) => a + b;
```

### Binary Content Support (NEW!)

Return rich content like Images from your tools and prompts.

```csharp
[McpTool]
public static CallToolResult GetSnapshot()
{
    return new CallToolResult 
    {
        Content = new List<ContentItem> 
        {
            new ImageContent { Data = "base64...", MimeType = "image/png" }
        }
    };
}
```

### OAuth Proxy

DotnetFastMCP includes a built-in **OAuth Proxy** that provides:

- ✅ **Dynamic Client Registration (DCR)** - Automatic client registration for MCP clients
- ✅ **Authorization Code Flow** - Full OAuth 2.0 authorization code flow with PKCE
- ✅ **Token Management** - Automatic token exchange, refresh, and revocation
- ✅ **Discovery Endpoints** - RFC 8414 compliant OAuth discovery

**Automatically Available Endpoints:**
- `/.well-known/oauth-authorization-server` - OAuth server metadata
- `/oauth/authorize` - Authorization endpoint
- `/oauth/token` - Token endpoint
- `/oauth/register` - Dynamic client registration
- `/oauth/userinfo` - User information endpoint

### Custom Scopes

Override default scopes for any provider:

```csharp
builder.AddAzureAdTokenVerifier(new AzureAdAuthOptions
{
    RequiredScopes = new[] { "openid", "profile", "email", "User.Read", "Calendars.Read" }
});
```

### Multiple Authentication Schemes

```csharp
// Support multiple providers simultaneously
builder.AddAzureAdTokenVerifier();
builder.AddGoogleTokenVerifier();
builder.AddGitHubTokenVerifier();
```

## 🔐 Security Best Practices

### Development
- ✅ Use environment variables for secrets
- ✅ Never commit credentials to source control
- ✅ Use `.env` files for local development
- ✅ Test with short-lived tokens

### Production
- ✅ Use HTTPS for all communication
- ✅ Store secrets in Azure Key Vault / AWS Secrets Manager
- ✅ Enable MFA for OAuth providers
- ✅ Implement rate limiting
- ✅ Monitor authentication logs
- ✅ Use separate app registrations per environment
- ✅ Validate token scopes match required permissions

## 📦 NuGet Package

Install from NuGet (when published):

```bash
dotnet add package DotnetFastMCP
```

## 🤝 Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🔗 Resources

### Official Documentation
- [Model Context Protocol Specification](https://modelcontextprotocol.io)
- [JSON-RPC 2.0 Specification](https://www.jsonrpc.org/specification)
- [OAuth 2.0 RFC 6749](https://datatracker.ietf.org/doc/html/rfc6749)
- [OpenID Connect Core 1.0](https://openid.net/specs/openid-connect-core-1_0.html)

### Framework Documentation
- [Protocol Discovery Guide](Documentation/protocol-discovery-guide.md)
- [Client Library Guide](Documentation/client-library-guide.md)
- [Context & Interaction Guide](Documentation/context-interaction-guide.md)
- [SSE Transport Guide](Documentation/sse-transport-guide.md)
- [Stdio Transport Guide](Documentation/stdio-transport-guide.md)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [.NET 8.0 Documentation](https://docs.microsoft.com/en-us/dotnet/)

### Provider Documentation
- [Azure AD OAuth 2.0](https://learn.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-auth-code-flow)
- [Google OAuth 2.0](https://developers.google.com/identity/protocols/oauth2)
- [GitHub OAuth](https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps)
- [Auth0 Documentation](https://auth0.com/docs)
- [Okta Developer](https://developer.okta.com/docs/)
- [AWS Cognito](https://docs.aws.amazon.com/cognito/)

## 🐛 Issues & Support

For bug reports and feature requests, please use [GitHub Issues](https://github.com/tekspry/.NetFastMCP/issues).

## ✨ What's New

### v1.12.0 - MFA Support (Latest)
- 🛡️ **MFA Enforcement** - Require `mfa` AMR claim for sensitive tools
- ✅ **Granular Control** - Enable per-tool using `[AuthorizeMcpTool(RequireMfa=true)]`
- 🔒 **Enhanced Security** - Standards-based multi-factor authentication check

### v1.11.0 - Binary Content Support (Latest)
- ✅ **Polymorphic Content** - Support for mixed Text and Image responses
- ✅ **Image Support** - Return Base64 encoded images from tools
- ✅ **Multimodal Prompts** - Embbed images in prompts for LLM context

### v1.10.0 - Icons Support
- ✅ **Server Icons** - Define a brand icon for your MCP server
- ✅ **Tool/Resource Icons** - Visually distinguish capabilities
- ✅ **UI/UX Enhancement** - Enable richer client experiences

### v1.9.0 - Background Tasks
- ✅ **Fire-and-Forget** - Offload long-running operations from tools
- ✅ **Non-Blocking** - Return immediate responses to clients
- ✅ **Hosted Service** - Built-in queuing mechanism using Channels

### v1.8.0 - Storage Abstractions
- ✅ **State Persistence** - Tools can now persist data via `McpContext.Storage`
- ✅ **Pluggable Backends** - Swap in Redis/SQL/File storage easily
- ✅ **In-Memory Default** - Zero-config built-in storage for development

### v1.7.0 - Server Composition
- ✅ **Server Composition** - Mount other MCP servers as modules (Micro-MCPs)
- ✅ **Namespacing** - Automatically prefix imported tools (e.g., `github_createIssue`)
- ✅ **Zero-Overhead** - High-performance internal dictionary routing (O(1))

### v1.6.0 - Middleware Interception
- ✅ **Middleware Pipeline** - Intercept and modify requests/responses
- ✅ **Critical Fixes** - Resolved Stdio transport initialization deadlocks
- ✅ **Builder API** - Easy registration with `AddMcpMiddleware<T>`

### v1.5.0 - Native Client Library
- ✅ **McpClient** - Type-safe .NET client for consuming MCP servers
- ✅ **Transport Agnostic** - Support for both Stdio and SSE connections
- ✅ **Notification Handling** - Events for real-time logs and progress

### v1.4.0 - Server-Sent Events (SSE)
- ✅ **SSE Transport** - Real-time server-to-client streaming transport
- ✅ **Async Notifications** - Push logs and progress updates to HTTP clients

### v1.3.0 - Context & Interaction
- ✅ **Context System** - `McpContext` injection for logging and progress
- ✅ **IMcpSession** - Transport-agnostic interaction abstraction

### v1.2.0 - Protocol Discovery
- ✅ **Dynamic Discovery** - Auto-discovery of Tools, Resources, and Prompts
- ✅ **Prompts/List** - Full support for prompt templates

### v1.1.0 - Stdio Transport & Authentication
- ✅ **Stdio Transport** - Initial support for stdio communication
- 🔐 **6 OAuth Providers** - Azure AD, Google, GitHub, Auth0, Okta, AWS Cognito
- 🔐 **OAuth Proxy** - Built-in DCR support

### v1.0.0 - Core Framework
- ✅ Attribute-based API
- ✅ JSON-RPC 2.0 compliance
- ✅ ASP.NET Core integration

## 🎯 Roadmap

### Core Functionality
- [x] **Protocol Discovery** - Dynamic discovery of Tools, Resources, and Prompts
- [x] **Context & Interaction** - Access logging, progress reporting, and client sampling via `Context`### 💾 Storage Abstraction

FastMCP provides a built-in state management system.

```csharp
[McpTool("remember_me")]
public async Task<string> RememberMe(string name, McpContext context)
{
    await context.Storage.SetAsync("last_user", name);
    return $"I will remember you, {name}!";
}
```

-   **Interfaces**: `IMcpStorage` for custom persistence (Redis, File, etc).
-   **Default**: `InMemoryMcpStorage` (development only).
-   **Injection**: `builder.AddMcpStorage<MyStorage>()`.
** - Support for standard input/output transport (essential for Claude Desktop)
- [x] **SSE Transport** - Dedicated Server-Sent Events transport
- [x] **Client Library** - Native .NET client SDK for building MCP clients

### Advanced Features
- [x] **Middleware Interception** - Hooks for inspecting/modifying JSON-RPC messages
- [x] **Server Composition** - Ability to mount or import other MCP servers
- [x] **Storage Abstractions** - Interfaces for state persistence
- [x] **Background Tasks** - Patterns for long-running operations

### UI & Metadata
- [x] **Icons** - Support for tool and server icons
- [x] **Binary Content** - Helpers for handling Image and Audio content types
- [x] **Multi-factor authentication (MFA)** - For OAuth providers

---

**Made with ❤️ by the DotnetFastMCP team**

**⭐ Star this repo if you find it useful!**

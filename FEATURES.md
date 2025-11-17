# DotnetFastMCP Framework - Features

This document outlines the features implemented in the DotnetFastMCP framework, which is a native .NET implementation inspired by the Python-based FastMCP project.

## Core Features Implemented

### 1. **Simplified Server Creation**
- **Class**: `FastMCPServer`
- **Description**: A central application class that holds all MCP components.
- **Usage**: Create an instance with a server name, then register components with it.
- **File**: `src/FastMCP/Server/FastMCPServer.cs`

### 2. **Declarative Component Registration**

#### Tools (`[McpTool]` Attribute)
- **File**: `src/FastMCP/Attributes/McpToolAttribute.cs`
- **Description**: Marks a method as an MCP tool that can be executed by a model.
- **Features**:
  - Automatic schema generation from C# method signatures and XML documentation
  - Support for optional names and descriptions
  - Parameter binding from JSON-RPC requests
- **Example**:
```csharp
[McpTool]
public static int Add(int a, int b) => a + b;
```

#### Resources (`[McpResource]` Attribute)
- **File**: `src/FastMCP/Attributes/McpResourceAttribute.cs`
- **Description**: Marks a method as an MCP resource provider.
- **Features**:
  - Returns static or dynamic resource content
  - Identified by URI-based naming
- **Example**:
```csharp
[McpResource("resource://config")]
public static object GetConfig() => new { Version = "1.0.0" };
```

### 3. **MCP Protocol Implementation (JSON-RPC 2.0)**
- **File**: `src/FastMCP/Hosting/McpProtocolMiddleware.cs`
- **Description**: Core middleware that handles JSON-RPC 2.0 protocol.
- **Features**:
  - Receives JSON-RPC 2.0 requests on the `/mcp` endpoint
  - Parses and validates request format
  - Dispatches to appropriate tool methods
  - Binds JSON parameters to method arguments (both by-name and by-position)
  - Invokes methods and captures results
  - Returns properly formatted JSON-RPC 2.0 responses
  - Comprehensive error handling with standard JSON-RPC error codes
- **Supported Error Codes**:
  - `ParseError (-32700)`: JSON parse errors
  - `InvalidRequest (-32600)`: Invalid JSON-RPC structure
  - `MethodNotFound (-32601)`: Tool not found
  - `InvalidParams (-32602)`: Parameter binding errors
  - `InternalError (-32603)`: Method execution errors

### 4. **ASP.NET Core Integration**
- **File**: `src/FastMCP/Hosting/McpServerBuilder.cs`
- **Description**: Builder pattern for configuring and launching MCP servers.
- **Features**:
  - Built on modern .NET hosting conventions
  - Fluent API for server configuration
  - Automatic component discovery from assemblies
  - Integration with ASP.NET Core's Kestrel web server
  - Support for dependency injection and configuration
- **Usage**:
```csharp
var server = new FastMCPServer("MyServer");
var builder = McpServerBuilder.Create(server, args);
builder.WithComponentsFrom(Assembly.GetExecutingAssembly());
var app = builder.Build();
await app.RunAsync();
```

### 5. **OpenAPI/FastAPI Integration**
- **Project**: `src/FastMCP.OpenApi`
- **Classes**: `OpenApiMcpConverter`, `OpenApiToolProxy`
- **Description**: Automatically converts OpenAPI specifications into MCP tools.
- **Features**:
  - Loads OpenAPI documents from streams
  - Extracts operations and converts them to MCP tools
  - Maps HTTP methods and paths to tool names
  - Supports parameter extraction from OpenAPI schemas
  - Provides proxy mechanism for invoking external OpenAPI services
  - Extensible for custom HTTP request handling
- **Usage** (Future):
```csharp
using var openApiStream = new FileStream("openapi.json", FileMode.Open);
OpenApiMcpConverter.RegisterFromOpenApi(openApiStream, server, "https://api.example.com");
```

### 6. **Command-Line Interface (CLI)**
- **Project**: `src/FastMCP.CLI`
- **File**: `src/FastMCP.CLI/Program.cs`
- **Description**: Provides developer tools for MCP server management.
- **Implemented Commands**:
  - `fastmcp version`: Display version information
- **Future Commands** (Planned):
  - `fastmcp run`: Run an MCP server from a project
  - `fastmcp generate`: Generate MCP server scaffold
  - `fastmcp validate`: Validate MCP configuration files

### 7. **Example Project**
- **Project**: `examples/BasicServer`
- **Description**: Demonstrates how to create and run a simple MCP server with components split into multiple files (`Tools.cs`, `Resources.cs`) for better organization.
- **Components**:
  - **Tools (`Tools.cs`)**:
    - `Add`: Adds two integers.
    - `Multiply`: Multiplies two integers.
    - `Greet`: Returns a greeting string.
  - **Resources (`Resources.cs`)**:
    - `GetConfig`: Returns server configuration.
    - `GetFeatures`: Returns a list of supported features.
    - `GetServerTime`: Returns the current server time.
- **Usage**: `dotnet run --project examples/BasicServer/BasicServer.csproj`

### 8. **Integration Testing**
- **Scripts**: `RUN_AND_TEST.ps1`, `LAUNCH_TESTS.ps1`
- **Description**: A comprehensive PowerShell-based test suite that performs end-to-end validation of a running MCP server.
- **Features**:
  - Starts the `BasicServer` automatically.
  - Waits for the server to be available.
  - Executes a series of tests against the live `/mcp` endpoint.
  - Validates successful responses and correct error handling.
  - Provides a detailed summary of passed and failed tests.
- **Tests Covered**:
  - Root endpoint health check (`GET /`)
  - Tool invocation with positional (array) and named (object) parameters
  - Resource retrieval for all defined resources
  - JSON-RPC error handling for non-existent methods

## Architecture Overview

### Project Structure
```
DotnetFastMCP/
├── src/
│   ├── FastMCP/                 # Core framework
│   │   ├── Attributes/          # Component markers
│   │   ├── Hosting/             # Server hosting and middleware
│   │   ├── Protocol/            # JSON-RPC data structures
│   │   └── Server/              # Server definition
│   ├── FastMCP.CLI/             # Command-line tools
│   └── FastMCP.OpenApi/         # OpenAPI integration
├── examples/
│   └── BasicServer/             # Sample MCP server
├── tests/                        # Test projects (future)
└── RUN_AND_TEST.ps1              # Main integration test script
└── LAUNCH_TESTS.ps1              # Launcher for the test script
```

### Protocol Layer
- **Request Format**: JSON-RPC 2.0 with method name, optional parameters, and request ID
- **Response Format**: JSON-RPC 2.0 with result, error, or both, and matching request ID
- **Endpoint**: HTTP POST to `/mcp`
- **Content-Type**: `application/json`

### Component Discovery
- Uses C# reflection to scan assemblies for methods decorated with `[McpTool]` and `[McpResource]`
- Runs at server startup via `McpServerBuilder.WithComponentsFrom(Assembly)`
- Automatically extracts method names, parameters, and documentation

## Future Enhancements

1. **Advanced Authentication**
   - OAuth2 provider integration (Google, GitHub, Azure, Auth0)
   - Token validation and session management
   - Per-tool authorization policies

2. **Configuration-Driven Deployment**
   - `fastmcp.json` configuration file support
   - Environment management (virtual environments, dependency installation)
   - Multi-transport runtime configuration

3. **Additional Transports**
   - STDIO transport for subprocess communication
   - SSE (Server-Sent Events) for streaming
   - gRPC support (optional)

4. **Proxying Capabilities**
   - Act as a proxy to another MCP server
   - Request forwarding and transformation

5. **Advanced OpenAPI Features**
   - Complex parameter type mapping (nested objects, arrays)
   - Authentication header injection
   - Request/response transformation middleware

6. **Development Tools**
   - Interactive CLI for testing tools
   - OpenAPI specification generation from MCP tools
   - Server introspection and documentation generation

7. **Performance Optimizations**
   - Caching of reflected component information
   - Compiled parameter binding expressions
   - Connection pooling for OpenAPI calls

## Design Principles

1. **Idiomatic .NET**: Uses C# attributes, async/await, dependency injection, and modern best practices
2. **Low Overhead**: Minimal abstraction layers to ensure good performance
3. **Extensibility**: Open for extension through inheritance, composition, and middleware patterns
4. **Developer Experience**: Fluent APIs and clear error messages for ease of use
5. **Standards Compliance**: Strict adherence to JSON-RPC 2.0 and MCP protocol specifications

## Getting Started

### Creating Your First Server

Follow these steps to create a server with organized components.

```csharp
// 1. Create a new console application project
// 2. Add a reference to the FastMCP NuGet package
// 3. Create your component files (e.g., Tools.cs, Resources.cs)

// File: Tools.cs
using FastMCP.Attributes;
public static class MyTools
{
    [McpTool]
    public static string Greet(string name) => $"Hello, {name}!";
}

// File: Resources.cs
using FastMCP.Attributes;
public static class MyResources
{
    [McpResource("resource://status")]
    public static object GetStatus() => new { online = true };
}

// 4. Configure and run the server in Program.cs

using FastMCP.Hosting;
using FastMCP.Server;
using System.Reflection;

var server = new FastMCPServer("Greeting Service");
var builder = McpServerBuilder.Create(server, args);
builder.WithComponentsFrom(Assembly.GetExecutingAssembly()); // Discovers all components
var app = builder.Build();
await app.RunAsync();
```

### Testing Tools

Send a JSON-RPC 2.0 POST request to `http://localhost:5000/mcp`:

```json
{
  "jsonrpc": "2.0",
  "method": "Greet",
  "params": { "name": "World" },
  "id": 1
}
```

Expected response:

```json
{
  "jsonrpc": "2.0",
  "result": "Hello, World!",
  "id": 1
}
```

## Development Status

- ✅ Core framework scaffolding
- ✅ JSON-RPC 2.0 protocol handling
- ✅ Component discovery and registration
- ✅ Example project with multiple components
- ✅ Integration test suite (PowerShell)
- ✅ OpenAPI integration foundation
- ✅ CLI skeleton
- 🔄 Enhanced OpenAPI HTTP invocation
- 🔄 Comprehensive error handling refinements
- ⏳ Authentication providers
- ⏳ Configuration files
- ⏳ Additional transports
- ⏳ Advanced middleware

---

For more information, visit the [GitHub repository](https://github.com/your-org/DotnetFastMCP).

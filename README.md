# DotnetFastMCP - Model Context Protocol (MCP) Server Framework

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![GitHub](https://img.shields.io/badge/GitHub-tekspry-black)](https://github.com/tekspry/.NetFastMCP)

A modern, production-ready C#/.NET framework for building Model Context Protocol (MCP) servers with minimal boilerplate and maximum flexibility.

## 🎯 Overview

DotnetFastMCP provides a clean, attribute-based approach to building MCP servers that implement the JSON-RPC 2.0 protocol. Built on ASP.NET Core, it leverages modern .NET features for high performance and reliability.

### Key Features

- ✅ **Simple Attribute-Based API** - Declare tools and resources with `[McpTool]` and `[McpResource]` attributes
- ✅ **Automatic Component Discovery** - Reflection-based scanning of assemblies for components
- ✅ **JSON-RPC 2.0 Compliant** - Full protocol compliance with proper error handling
- ✅ **Flexible Parameter Binding** - Supports both array and named parameters
- ✅ **Built on ASP.NET Core** - Leverage the powerful ASP.NET Core hosting model
- ✅ **Production Ready** - Comprehensive error handling and logging
- ✅ **Type-Safe** - Full C# type system integration

## 🚀 Quick Start

### Installation

Clone the repository:
```bash
git clone https://github.com/tekspry/.NetFastMCP.git
cd DotnetFastMCP
```

### Building

```bash
dotnet build -c Release
```

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

## 🔧 Creating an MCP Server

### 1. Define Components

For better organization, split your components into multiple files (e.g., `Tools.cs`, `Resources.cs`). The framework will discover them automatically.

**File: `Tools.cs`**
```csharp
using FastMCP.Attributes;

public static class Tools
{
    /// <summary>
    /// Adds two integers together.
    /// </summary>
    [McpTool]
    public static int Add(int a, int b) => a + b;

    /// <summary>
    /// Returns a greeting for the given name.
    /// </summary>
    [McpTool]
    public static string Greet(string name) => $"Hello, {name}!";
}
```

**File: `Resources.cs`**
```csharp
using FastMCP.Attributes;

public static class Resources
{
    /// <summary>
    /// Returns server configuration.
    /// </summary>
    [McpResource("resource://config")]
    public static object GetConfig()
    {
        return new { Version = "1.0.0", Author = "Your Team" };
    }
}
```

### 2. Create Server

Initialize the MCP server in `Program.cs`:

```csharp
using FastMCP.Hosting;
using FastMCP.Server;
using System.Reflection;

var mcpServer = new FastMCPServer(name: "My MCP Server");
var builder = McpServerBuilder.Create(mcpServer, args);

// Register components from assembly
builder.WithComponentsFrom(Assembly.GetExecutingAssembly());

var app = builder.Build();
await app.RunAsync();
```

### 3. Deploy

Build and publish:

```bash
dotnet publish -c Release
```

## 📡 JSON-RPC Protocol

### Calling Tools

**Request:**
```json
{
  "jsonrpc": "2.0",
  "method": "Add",
  "params": [5, 3],
  "id": 1
}
```

**Response:**
```json
{
  "result": 8,
  "jsonrpc": "2.0",
  "id": 1
}
```

### Named Parameters

**Request:**
```json
{
  "jsonrpc": "2.0",
  "method": "Add",
  "params": {"a": 10, "b": 20},
  "id": 2
}
```

**Response:**
```json
{
  "result": 30,
  "jsonrpc": "2.0",
  "id": 2
}
```

### Error Handling

**Request (Invalid):**
```json
{
  "jsonrpc": "2.0",
  "method": "NonExistent",
  "params": [],
  "id": 3
}
```

**Response (Error):**
```json
{
  "error": {
    "code": -32601,
    "message": "Method 'NonExistent' not found."
  },
  "jsonrpc": "2.0",
  "id": 3
}
```

## 🧪 Testing

### Run Unit & Integration Tests

```bash
dotnet test
```

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

```powershell
# Test Add tool with array parameters
$body = @{jsonrpc="2.0";method="Add";params=@(5,3);id=1} | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:5000/mcp" -Method Post `
  -Body $body -ContentType "application/json"

# Expected response: {"result": 8, "jsonrpc": "2.0", "id": 1}
```

## 📖 Attributes

### [McpTool]

Declares a method as an MCP tool:

```csharp
[McpTool]
public static int Add(int a, int b) => a + b;

[McpTool("CustomName")]  // Optional: specify custom name
public static string Process(string data) => data.ToUpper();
```

**Parameters:**
- `Name` (optional) - Custom method name. Defaults to method name.

### [McpResource]

Declares a method as an MCP resource:

```csharp
[McpResource("resource://config")]
public static object GetConfig() => new { Version = "1.0.0" };
```

**Parameters:**
- `Uri` (required) - Resource URI in format `resource://name`

## 🔐 Security

The framework includes:

- ✅ **Input Validation** - Parameter type checking and conversion
- ✅ **Error Handling** - Comprehensive exception handling with proper error codes
- ✅ **JSON-RPC Compliance** - Specification-compliant error responses
- ✅ **Type Safety** - C# type system prevents many common errors

For production deployments:
- Use HTTPS for communication
- Implement authentication as needed
- Validate all input parameters
- Monitor error logs

## 🏗️ Advanced Usage

### Custom Hosting Configuration

```csharp
var builder = McpServerBuilder.Create(mcpServer, args);

// Configure via WebApplicationBuilder if needed
// builder._webAppBuilder.Services.AddMyService();

builder.WithComponentsFrom(Assembly.GetExecutingAssembly());
var app = builder.Build();
```

### Middleware Integration

The framework automatically registers the MCP protocol middleware:
- Handles `/mcp` POST endpoints
- Routes requests to registered tools/resources
- Validates JSON-RPC format
- Returns proper error responses

## 📝 API Endpoints

### GET `/`

Returns server metadata. The example server returns:

```
GET http://localhost:5000/
HTTP/1.1 200 OK

MCP Server 'My First Dotnet MCP Server' is running.
Registered Tools: 3
Registered Resources: 3
```

### POST `/mcp`

Processes JSON-RPC requests:

```
POST http://localhost:5000/mcp
Content-Type: application/json

{
  "jsonrpc": "2.0",
  "method": "MethodName",
  "params": [...],
  "id": 1
}
```

## 🛠️ Development

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 / VS Code / Rider (recommended)
- Git

### Project Setup

```bash
# Clone repository
git clone https://github.com/tekspry/.NetFastMCP.git
cd DotnetFastMCP

# Restore dependencies
dotnet restore

# Build
dotnet build -c Debug

# Run example server
cd examples/BasicServer
dotnet run
```

### Building Documentation

```bash
# Build example
cd examples/BasicServer
dotnet build
```

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

- [Model Context Protocol Specification](https://modelcontextprotocol.io)
- [JSON-RPC 2.0 Specification](https://www.jsonrpc.org/specification)
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [.NET 8.0 Documentation](https://docs.microsoft.com/en-us/dotnet/)

## 🐛 Issues & Support

For bug reports and feature requests, please use [GitHub Issues](https://github.com/tekspry/.NetFastMCP/issues).

## ✨ Acknowledgments

Built as a .NET implementation of the Model Context Protocol, enabling seamless integration with AI models and context-aware applications.

---

**Made with ❤️ by the DotnetFastMCP team**

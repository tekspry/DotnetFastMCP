# DotnetFastMCP v2.2.0 Manual Testing Guide

This guide details how to manually test and verify the new Multimodal features (`McpMediaLoader` and `McpContent`) without making any paid GenAI service calls.

---

## 🛡️ Cost & Safety Notice
**None of the test steps below call any external AI services (no Azure OpenAI, no Gemini, no Replicate, no Kling). All tests are 100% offline or use local loopback/mock HTTP servers with $0 cost.**

---

## Step 1: Automated Test Suite Verification

Run the full dual-targeted unit test suite (.NET 8 LTS & .NET 10 LTS):

```powershell
cd c:\pocs\FastMCP\DotnetFastMCP
dotnet test tests/FastMCP.Tests/FastMCP.Tests.csproj
```

**Expected Outcome:**
- 126 tests passed on `.NET 10 LTS`
- 126 tests passed on `.NET 8 LTS`
- Total: 252 tests passed, 0 failed, 0 skipped.
- New test classes verified:
  - `FastMCP.Tests.Multimodal.McpMediaLoaderTests` (22 tests)
  - `FastMCP.Tests.Multimodal.McpContentTests` (19 tests)

---

## Step 2: Manual Verification of `McpMediaLoader` in C#

You can test `McpMediaLoader` directly with a quick one-line script or in `csharp` interactive / test runner:

### 1. Test Base64 Data URI Fast-Path (Zero Network Call, Zero Heap Allocation)
```csharp
using FastMCP;

// 1x1 transparent PNG data URI
var dataUri = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=";
var (base64, mime) = await McpMediaLoader.LoadAsBase64Async(dataUri);

Console.WriteLine($"MIME: {mime}"); // Expected: image/png
Console.WriteLine($"Base64 length: {base64.Length}"); // Expected: 84
```

### 2. Test Local File Path & Magic-Byte Sniffing
```csharp
using FastMCP;

// Create a temporary PNG file with valid header bytes
var tempPath = Path.Combine(Path.GetTempPath(), "test_sample.png");
byte[] pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01 };
await File.WriteAllBytesAsync(tempPath, pngBytes);

// Test local file resolution
var (base64, mime) = await McpMediaLoader.LoadAsBase64Async(tempPath);
Console.WriteLine($"MIME: {mime}"); // Expected: image/png

// Test file:// schema resolution
var (fileUriB64, fileUriMime) = await McpMediaLoader.LoadAsBase64Async($"file:///{tempPath.Replace('\\', '/')}");
Console.WriteLine($"File URI MIME: {fileUriMime}"); // Expected: image/png

// Clean up
File.Delete(tempPath);
```

### 3. Test Negative / Error Handling
```csharp
using FastMCP;

// Non-existent file throws FileNotFoundException
try
{
    await McpMediaLoader.LoadAsBase64Async("C:\\non_existent_folder_xyz\\does_not_exist.png");
}
catch (FileNotFoundException ex)
{
    Console.WriteLine($"Caught expected exception: {ex.Message}");
}
```

---

## Step 3: Verify Microservices Build (FashionAccessoryPipeline)

Verify that the 4 MCP microservices compile and reference `FastMCP` without any build errors:

```powershell
cd c:\pocs\FastMCP\FashionAccessoryPipeline

# 1. Vision MCP Server
dotnet build src/McpServers/FashionPipeline.VisionMcpServer/FashionPipeline.VisionMcpServer.csproj

# 2. Prompt MCP Server
dotnet build src/McpServers/FashionPipeline.PromptMcpServer/FashionPipeline.PromptMcpServer.csproj

# 3. Inpainting MCP Server
dotnet build src/McpServers/FashionPipeline.InpaintingMcpServer/FashionPipeline.InpaintingMcpServer.csproj

# 4. Image MCP Server
dotnet build src/McpServers/FashionPipeline.ImageMcpServer/FashionPipeline.ImageMcpServer.csproj
```

**Expected Outcome:**
All 4 projects compile with **0 Errors**.

---

## Step 4: Verify Local NuGet Package

Verify that the `DotnetFastMCP.2.2.0.nupkg` package file is present and properly formed:

```powershell
Get-Item c:\pocs\FastMCP\DotnetFastMCP\bin\packages\DotnetFastMCP.2.2.0.nupkg
```

**Expected Outcome:**
The package is present (~300 KB) containing both `net8.0` and `net10.0` assemblies and `README.md`.

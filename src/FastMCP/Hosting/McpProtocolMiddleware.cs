using FastMCP.Attributes;
using FastMCP.Protocol;
using FastMCP.Server;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using System.Threading.Tasks;

namespace FastMCP.Hosting;

public class McpProtocolMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public McpProtocolMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, FastMCPServer server)
    {
        try
        {
            Console.WriteLine($"[Middleware] Processing request: {context.Request.Method} {context.Request.Path}");
            
            // Only process MCP requests to /mcp endpoint
            if (context.Request.Path != "/mcp" || !HttpMethods.IsPost(context.Request.Method))
            {
                Console.WriteLine($"[Middleware] Not an MCP request, passing to next middleware");
                await _next(context);
                return;
            }

            Console.WriteLine("[Middleware] Processing MCP request...");
            context.Response.ContentType = "application/json";
            JsonRpcRequest? request;
            try
            {
                request = await JsonSerializer.DeserializeAsync<JsonRpcRequest>(context.Request.Body, _jsonOptions);
                Console.WriteLine($"[Middleware] Deserialized request: method={request?.Method}");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"[Middleware] JSON parse error: {ex.Message}");
                var response = JsonRpcResponse.FromError(
                    JsonRpcError.ErrorCodes.ParseError, 
                    $"JSON parse error: {ex.Message}", 
                    null);
                await JsonSerializer.SerializeAsync(context.Response.Body, response, _jsonOptions);
                return;
            }

            if (request is null || request.JsonRpc != "2.0" || string.IsNullOrEmpty(request.Method))
            {
                Console.WriteLine("[Middleware] Invalid request format");
                var response = JsonRpcResponse.FromError(
                    JsonRpcError.ErrorCodes.InvalidRequest, 
                    "Invalid JSON-RPC request.", 
                    request?.Id);
                await JsonSerializer.SerializeAsync(context.Response.Body, response, _jsonOptions);
                return;
            }

            // Find method in Tools, then Resources
            var toolMethod = server.Tools.FirstOrDefault(t =>
                (t.GetCustomAttribute<McpToolAttribute>()?.Name ?? t.Name)
                    .Equals(request.Method, StringComparison.OrdinalIgnoreCase));
            
            if (toolMethod is null)
            {
                // For resources, use the method name for invocation (not the URI which is metadata)
                toolMethod = server.Resources.FirstOrDefault(t =>
                    t.Name.Equals(request.Method, StringComparison.OrdinalIgnoreCase));
            }

            if (toolMethod is null)
            {
                Console.WriteLine($"[Middleware] Method '{request.Method}' not found");
                var response = JsonRpcResponse.FromError(
                    JsonRpcError.ErrorCodes.MethodNotFound, 
                    $"Method '{request.Method}' not found.", 
                    request.Id);
                await JsonSerializer.SerializeAsync(context.Response.Body, response, _jsonOptions);
                return;
            }

            object?[] args;
            try
            {
                Console.WriteLine($"[Middleware] Binding parameters for {request.Method}...");
                args = BindParameters(toolMethod, request.Params);
                Console.WriteLine($"[Middleware] Parameter binding successful");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Middleware] Parameter binding error: {ex.Message}");
                var response = JsonRpcResponse.FromError(
                    JsonRpcError.ErrorCodes.InvalidParams, 
                    $"Invalid parameters: {ex.Message}", 
                    request.Id);
                await JsonSerializer.SerializeAsync(context.Response.Body, response, _jsonOptions);
                return;
            }

            try
            {
                Console.WriteLine($"[Middleware] Invoking method {request.Method}...");
                var result = toolMethod.Invoke(null, args);
                Console.WriteLine($"[Middleware] Method invocation successful, result={result}");
                var response = new JsonRpcResponse { Id = request.Id, Result = result };
                await JsonSerializer.SerializeAsync(context.Response.Body, response, _jsonOptions);
                Console.WriteLine("[Middleware] Response sent successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Middleware] Method execution error: {ex.InnerException?.Message ?? ex.Message}");
                var response = JsonRpcResponse.FromError(
                    JsonRpcError.ErrorCodes.InternalError, 
                    $"Method execution error: {ex.InnerException?.Message ?? ex.Message}", 
                    request.Id);
                await JsonSerializer.SerializeAsync(context.Response.Body, response, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Middleware] UNHANDLED EXCEPTION: {ex}");
            Console.WriteLine($"[Middleware] Stack: {ex.StackTrace}");
            try
            {
                context.Response.ContentType = "application/json";
                var errorResponse = JsonRpcResponse.FromError(
                    JsonRpcError.ErrorCodes.InternalError, 
                    "Internal server error", 
                    null);
                await JsonSerializer.SerializeAsync(context.Response.Body, errorResponse, _jsonOptions);
            }
            catch
            {
                // If we can't send a response, the connection will be closed anyway
            }
        }
    }

    /// <summary>
    /// Binds JSON-RPC parameters to method parameters.
    /// Supports both array-style (positional) and object-style (named) parameters.
    /// </summary>
    private object?[] BindParameters(MethodInfo method, object? rpcParams)
    {
        var methodParams = method.GetParameters();
        if (methodParams.Length == 0) 
            return Array.Empty<object>();

        var args = new object?[methodParams.Length];
        
        // Handle null parameters
        if (rpcParams is null)
        {
            for (int i = 0; i < methodParams.Length; i++)
            {
                var param = methodParams[i];
                if (param.HasDefaultValue)
                {
                    args[i] = param.DefaultValue;
                }
                else
                {
                    throw new InvalidOperationException($"Missing required parameter: {param.Name}");
                }
            }
            return args;
        }

        // Convert to JsonElement if needed
        if (rpcParams is not JsonElement paramsElement)
        {
            var json = JsonSerializer.Serialize(rpcParams);
            using var doc = JsonDocument.Parse(json);
            paramsElement = doc.RootElement;
            
            // Handle object and array immediately within using scope
            if (paramsElement.ValueKind == JsonValueKind.Object)
            {
                return BindObjectParameters(paramsElement, methodParams);
            }
            else if (paramsElement.ValueKind == JsonValueKind.Array)
            {
                return BindArrayParameters(paramsElement, methodParams);
            }
        }

        // Process JsonElement directly
        if (paramsElement.ValueKind == JsonValueKind.Object)
        {
            return BindObjectParameters(paramsElement, methodParams);
        }
        else if (paramsElement.ValueKind == JsonValueKind.Array)
        {
            return BindArrayParameters(paramsElement, methodParams);
        }
        else
        {
            throw new InvalidOperationException("Parameters must be an object or an array.");
        }
    }

    private object?[] BindObjectParameters(JsonElement paramsElement, ParameterInfo[] methodParams)
    {
        var args = new object?[methodParams.Length];
        for (int i = 0; i < methodParams.Length; i++)
        {
            var param = methodParams[i];
            var paramProp = paramsElement.EnumerateObject()
                .FirstOrDefault(p => p.Name.Equals(param.Name, StringComparison.OrdinalIgnoreCase));
            
            if (!paramProp.Equals(default))
            {
                args[i] = paramProp.Value.Deserialize(param.ParameterType, _jsonOptions);
            }
            else if (param.HasDefaultValue)
            {
                args[i] = param.DefaultValue;
            }
            else
            {
                throw new InvalidOperationException($"Missing required parameter: {param.Name}");
            }
        }
        return args;
    }

    private object?[] BindArrayParameters(JsonElement paramsElement, ParameterInfo[] methodParams)
    {
        var args = new object?[methodParams.Length];
        var paramsArray = paramsElement.EnumerateArray().ToList();
        
        for (int i = 0; i < methodParams.Length; i++)
        {
            if (i < paramsArray.Count)
            {
                args[i] = paramsArray[i].Deserialize(methodParams[i].ParameterType, _jsonOptions);
            }
            else if (methodParams[i].HasDefaultValue)
            {
                args[i] = methodParams[i].DefaultValue;
            }
            else
            {
                throw new InvalidOperationException($"Missing required parameter: {methodParams[i].Name}");
            }
        }
        return args;
    }
}

public static class McpServerExtensions
{
    public static IApplicationBuilder UseMcpProtocol(this IApplicationBuilder app)
    {
        return app.UseMiddleware<McpProtocolMiddleware>();
    }
}

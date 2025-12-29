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
using Microsoft.AspNetCore.Authorization; 
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace FastMCP.Hosting;

public class McpProtocolMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAuthorizationService _authorizationService;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    
    public McpProtocolMiddleware(RequestDelegate next, IAuthorizationService authorizationService)
    {
        _next = next;
        _authorizationService = authorizationService;
    }

    public async Task InvokeAsync(HttpContext context, FastMCPServer server)
    {
        try
        {
            if (!IsValidMcpRequest(context))
            {
                await _next(context);
                return;
            }

            Console.WriteLine("[Middleware] Processing MCP request...");
            context.Response.ContentType = "application/json";

            var request = await ParseJsonRpcRequestAsync(context);
            if (request == null) return; // Error response already sent by parser

            switch (request.Method)
            {
                case "prompts/list":
                    await HandlePromptsListAsync(context, server, request);
                    break;
                case "prompts/get":
                    await HandlePromptsGetAsync(context, server, request);
                    break;
                default:
                    await HandleToolExecutionAsync(context, server, request);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Middleware] UNHANDLED EXCEPTION: {ex}");
            await SendErrorResponseAsync(context, JsonRpcError.ErrorCodes.InternalError, "Internal server error", null);
        }
    }

    private bool IsValidMcpRequest(HttpContext context)
    {
        // Only process MCP requests to /mcp endpoint
        if (context.Request.Path != "/mcp" || !HttpMethods.IsPost(context.Request.Method))
        {
            return false;
        }
        return true;
    }

    private async Task<JsonRpcRequest?> ParseJsonRpcRequestAsync(HttpContext context)
    {
        try
        {
            var request = await JsonSerializer.DeserializeAsync<JsonRpcRequest>(context.Request.Body, _jsonOptions);
            Console.WriteLine($"[Middleware] Deserialized request: method={request?.Method}");

            if (request is null || request.JsonRpc != "2.0" || string.IsNullOrEmpty(request.Method))
            {
                Console.WriteLine("[Middleware] Invalid request format");
                await SendErrorResponseAsync(context, JsonRpcError.ErrorCodes.InvalidRequest, "Invalid JSON-RPC request.", request?.Id);
                return null;
            }

            return request;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[Middleware] JSON parse error: {ex.Message}");
            await SendErrorResponseAsync(context, JsonRpcError.ErrorCodes.ParseError, $"JSON parse error: {ex.Message}", null);
            return null;
        }
    }

    private async Task HandlePromptsListAsync(HttpContext context, FastMCPServer server, JsonRpcRequest request)
    {
        try 
        {
            var prompts = server.Prompts.Select(m => {
                var attr = m.GetCustomAttribute<McpPromptAttribute>();
                return new Prompt 
                {
                    Name = attr?.Name ?? m.Name,
                    Description = attr?.Description,
                    Arguments = m.GetParameters()
                        .Where(p => p.ParameterType != typeof(ClaimsPrincipal))
                        .Select(p => new PromptArgument
                        {
                            Name = p.Name ?? "arg",
                            Description = "",
                            Required = !p.HasDefaultValue
                        }).ToList()
                };
            }).ToList();

            var response = new JsonRpcResponse { Id = request.Id, Result = new { prompts } };
            await JsonSerializer.SerializeAsync(context.Response.Body, response, _jsonOptions);
        }
        catch (Exception ex)
        {
            await SendErrorResponseAsync(context, JsonRpcError.ErrorCodes.InternalError, ex.Message, request.Id);
        }
    }

    private async Task HandlePromptsGetAsync(HttpContext context, FastMCPServer server, JsonRpcRequest request)
    {
        try
        {
            // Parse parameters
            if (request.Params is not JsonElement root || root.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException("Invalid params for prompts/get");
            }
            if (!root.TryGetProperty("name", out var nameProp))
            {
                throw new ArgumentException("Missing 'name' parameter");
            }
            string promptName = nameProp.GetString() ?? "";

            // Find prompt
            var promptMethod = server.Prompts.FirstOrDefault(m => 
                (m.GetCustomAttribute<McpPromptAttribute>()?.Name ?? m.Name)
                .Equals(promptName, StringComparison.OrdinalIgnoreCase));

            if (promptMethod == null)
            {
                await SendErrorResponseAsync(context, JsonRpcError.ErrorCodes.MethodNotFound, $"Prompt '{promptName}' not found", request.Id);
                return;
            }

            // Get arguments
            JsonElement arguments = default;
            if (root.TryGetProperty("arguments", out var argsProp))
            {
                arguments = argsProp;
            }
            else
            {
                arguments = JsonDocument.Parse("{}").RootElement;
            }

            // Bind and Invoke
            var args = BindParameters(promptMethod, arguments, context.User);
            var result = await InvokeMethodAsync(promptMethod, null, args); // Prompts are static

            var response = new JsonRpcResponse { Id = request.Id, Result = result };
            await JsonSerializer.SerializeAsync(context.Response.Body, response, _jsonOptions);
        }
        catch (Exception ex)
        {
            await SendErrorResponseAsync(context, JsonRpcError.ErrorCodes.InternalError, ex.InnerException?.Message ?? ex.Message, request.Id);
        }
    }

    private async Task HandleToolExecutionAsync(HttpContext context, FastMCPServer server, JsonRpcRequest request)
    {
        // 1. Discovery
        MethodInfo? toolMethod = null;
        object? instance = null;

        toolMethod = server.Tools.FirstOrDefault(t =>
            (t.GetCustomAttribute<McpToolAttribute>()?.Name ?? t.Name)
                .Equals(request.Method, StringComparison.OrdinalIgnoreCase));

        if (toolMethod is null)
        {
            toolMethod = server.Resources.FirstOrDefault(t =>
                (t.GetCustomAttribute<McpResourceAttribute>()?.Uri?.Split('/').Last() ?? t.Name)
                    .Equals(request.Method, StringComparison.OrdinalIgnoreCase));
        }

        if (toolMethod is null && server.DynamicTools.TryGetValue(request.Method, out var dynamicToolHandler))
        {
            if (dynamicToolHandler is IDynamicMcpToolHandler mcpDynamicToolHandler)
            {
                toolMethod = mcpDynamicToolHandler.GetInvocationMethodInfo();
                instance = mcpDynamicToolHandler;
            }
        }

        if (toolMethod is null)
        {
            Console.WriteLine($"[Middleware] Method '{request.Method}' not found");
            await SendErrorResponseAsync(context, JsonRpcError.ErrorCodes.MethodNotFound, $"Method '{request.Method}' not found.", request.Id);
            return;
        }

        // 2. Authorization
        if (!await AuthorizeMethodAsync(context, toolMethod, instance))
        {
            return; // 401 response handled inside AuthorizeMethodAsync
        }

        // 3. Binding
        object?[] args;
        try
        {
            if (instance is IDynamicMcpToolHandler && request.Params is JsonElement)
            {
                args = new object?[] { request.Params };
            }
            else
            {
                args = BindParameters(toolMethod, request.Params, context.User);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Middleware] Parameter binding error: {ex.Message}");
            await SendErrorResponseAsync(context, JsonRpcError.ErrorCodes.InvalidParams, $"Invalid parameters: {ex.Message}", request.Id);
            return;
        }

        // 4. Invocation
        try
        {
            var result = await InvokeMethodAsync(toolMethod, instance, args);
            var response = new JsonRpcResponse { Id = request.Id, Result = result };
            await JsonSerializer.SerializeAsync(context.Response.Body, response, _jsonOptions);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Middleware] Method execution error: {ex.InnerException?.Message ?? ex.Message}");
            await SendErrorResponseAsync(context, JsonRpcError.ErrorCodes.InternalError, $"Method execution error: {ex.InnerException?.Message ?? ex.Message}", request.Id);
        }
    }

    private async Task<bool> AuthorizeMethodAsync(HttpContext context, MethodInfo method, object? instance)
    {
        var authorizeAttribute = method.GetCustomAttribute<AuthorizeMcpToolAttribute>();
        var standardAuthorizeAttribute = method.GetCustomAttribute<AuthorizeAttribute>();
        
        if (authorizeAttribute == null && standardAuthorizeAttribute == null)
        {
            return true;
        }

        Console.WriteLine($"[Middleware] Authorization required for method. User: {context.User.Identity?.Name ?? "Anonymous"}");
        
        var policyBuilder = new AuthorizationPolicyBuilder();

        if (authorizeAttribute != null)
        {
            if (!string.IsNullOrEmpty(authorizeAttribute.Policy))
                policyBuilder.AddRequirements(new PolicyNameRequirement(authorizeAttribute.Policy));
            if (!string.IsNullOrEmpty(authorizeAttribute.Roles))
                policyBuilder.RequireRole(authorizeAttribute.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries));
            if (!string.IsNullOrEmpty(authorizeAttribute.AuthenticationSchemes))
                policyBuilder.AddAuthenticationSchemes(authorizeAttribute.AuthenticationSchemes.Split(',', StringSplitOptions.RemoveEmptyEntries));
        }
        
        if (standardAuthorizeAttribute != null)
        {
            if (!string.IsNullOrEmpty(standardAuthorizeAttribute.Policy))
                policyBuilder.AddRequirements(new PolicyNameRequirement(standardAuthorizeAttribute.Policy));
            if (!string.IsNullOrEmpty(standardAuthorizeAttribute.Roles))
                policyBuilder.RequireRole(standardAuthorizeAttribute.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries));
            if (standardAuthorizeAttribute.AuthenticationSchemes != null)
                policyBuilder.AddAuthenticationSchemes(standardAuthorizeAttribute.AuthenticationSchemes.Split(',', StringSplitOptions.RemoveEmptyEntries));
        }

        if (!policyBuilder.Requirements.Any() && policyBuilder.AuthenticationSchemes.Count == 0)
        {
            policyBuilder.RequireAuthenticatedUser();
        }

        var policy = policyBuilder.Build();
        var authorizationResult = await _authorizationService.AuthorizeAsync(context.User, instance, policy);

        if (!authorizationResult.Succeeded)
        {
            Console.WriteLine($"[Middleware] Authorization failed.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            // No JsonRpcResponse needed if we set 401 status mostly, but let's be consistent
            var errorResponse = JsonRpcResponse.FromError(
                JsonRpcError.ErrorCodes.InternalError,
                "Unauthorized or Forbidden.",
                null);
            await JsonSerializer.SerializeAsync(context.Response.Body, errorResponse, _jsonOptions);
            return false;
        }

        return true;
    }

    private async Task<object?> InvokeMethodAsync(MethodInfo method, object? instance, object?[] args)
    {
        var result = method.Invoke(instance, args);
        if (result is Task task)
        {
            await task.ConfigureAwait(false);
            var resultProperty = task.GetType().GetProperty("Result");
            return resultProperty?.GetValue(task);
        }
        return result;
    }

    private async Task SendErrorResponseAsync(HttpContext context, int code, string message, object? id)
    {
        var response = JsonRpcResponse.FromError(code, message, id);
        await JsonSerializer.SerializeAsync(context.Response.Body, response, _jsonOptions);
    }

    // --- Parameter Binding Helpers (Kept mostly as is) ---

    private object?[] BindParameters(MethodInfo method, object? rpcParams, ClaimsPrincipal? user = null)
    {
        var methodParams = method.GetParameters();
        if (methodParams.Length == 0) 
            return Array.Empty<object>();

        var args = new object?[methodParams.Length];
        
        // Inject framework parameters
        for (int i = 0; i < methodParams.Length; i++)
        {
            if (methodParams[i].ParameterType == typeof(ClaimsPrincipal))
            {
                args[i] = user;
            }
        }

        if (rpcParams is null)
        {
            FillMissingParamsWithDefaults(methodParams, args);
            return args;
        }

        if (rpcParams is not JsonElement paramsElement)
        {
            var json = JsonSerializer.Serialize(rpcParams);
            using var doc = JsonDocument.Parse(json);
            paramsElement = doc.RootElement;
            return BindJsonElement(paramsElement, methodParams, args);
        }

        return BindJsonElement((JsonElement)rpcParams, methodParams, args);
    }

    private object?[] BindJsonElement(JsonElement paramsElement, ParameterInfo[] methodParams, object?[] args)
    {
        if (paramsElement.ValueKind == JsonValueKind.Object)
        {
            BindObjectParameters(paramsElement, methodParams, args);
        }
        else if (paramsElement.ValueKind == JsonValueKind.Array)
        {
            BindArrayParameters(paramsElement, methodParams, args);
        }
        else
        {
            throw new InvalidOperationException("Parameters must be an object or an array.");
        }
        return args;
    }

    private void FillMissingParamsWithDefaults(ParameterInfo[] methodParams, object?[] args)
    {
        for (int i = 0; i < methodParams.Length; i++)
        {
            if (args[i] != null) continue;
            if (methodParams[i].HasDefaultValue)
                args[i] = methodParams[i].DefaultValue;
            else
                throw new InvalidOperationException($"Missing required parameter: {methodParams[i].Name}");
        }
    }

    private void BindObjectParameters(JsonElement paramsElement, ParameterInfo[] methodParams, object?[] args)
    {
        for (int i = 0; i < methodParams.Length; i++)
        {
            if (args[i] != null) continue; // Already injected

            var param = methodParams[i];
            var paramProp = paramsElement.EnumerateObject()
                .FirstOrDefault(p => p.Name.Equals(param.Name, StringComparison.OrdinalIgnoreCase));
            
            if (paramProp.Value.ValueKind != JsonValueKind.Undefined)
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
    }

    private void BindArrayParameters(JsonElement paramsElement, ParameterInfo[] methodParams, object?[] args)
    {
        var paramsArray = paramsElement.EnumerateArray().ToList();
        
        for (int i = 0; i < methodParams.Length; i++)
        {
            if (args[i] != null) continue; // Already injected

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
    }
}

public static class McpServerExtensions
{
    public static IApplicationBuilder UseMcpProtocol(this IApplicationBuilder app)
    {
        return app.UseMiddleware<McpProtocolMiddleware>();
    }
}

using FastMCP.Protocol;
using FastMCP.Server;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace FastMCP.Hosting;

public class McpSseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly McpSseSessionManager _sessionManager;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public McpSseMiddleware(RequestDelegate next, McpSseSessionManager sessionManager)
    {
        _next = next;
        _sessionManager = sessionManager;
    }

    public async Task InvokeAsync(HttpContext context, FastMCPServer server, McpRequestHandler requestHandler, ILogger<McpSseMiddleware> logger)
    {
        // 1. Handle SSE Connection (GET /sse)
        if (context.Request.Path == "/sse" && HttpMethods.IsGet(context.Request.Method))
        {
            await HandleSseConnectionAsync(context, logger);
            return;
        }

        // 2. Handle Messages (POST /message?sessionId=...)
        if (context.Request.Path == "/message" && HttpMethods.IsPost(context.Request.Method))
        {
            await HandleSseMessageAsync(context, server, requestHandler, logger);
            return;
        }

        await _next(context);
    }

    private async Task HandleSseConnectionAsync(HttpContext context, ILogger logger)
    {
        context.Response.Headers.Append("Content-Type", "text/event-stream");
        context.Response.Headers.Append("Cache-Control", "no-cache");
        context.Response.Headers.Append("Connection", "keep-alive");

        var session = new McpSseSession(context.Response, _jsonOptions);
        _sessionManager.AddSession(session);
        
        logger.LogInformation("SSE Session Connected: {Id}", session.Id);

        try
        {
            // Send the 'endpoint' event pointing to the message URL
            var endpointUri = $"/message?sessionId={session.Id}";
            await session.SendEndpointEventAsync(endpointUri, context.RequestAborted);

            // Keep connection open until client disconnects
            await Task.Delay(Timeout.Infinite, context.RequestAborted);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("SSE Session Disconnected: {Id}", session.Id);
        }
        finally
        {
            _sessionManager.RemoveSession(session.Id);
        }
    }

    private async Task HandleSseMessageAsync(HttpContext context, FastMCPServer server, McpRequestHandler handler, ILogger logger)
    {
        string? sessionId = context.Request.Query["sessionId"];
        if (string.IsNullOrEmpty(sessionId) || _sessionManager.GetSession(sessionId) is not { } session)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("Session not found");
            return;
        }

        try
        {
            var request = await JsonSerializer.DeserializeAsync<JsonRpcRequest>(context.Request.Body, _jsonOptions);
            if (request == null) return;

            // Execute the request
            // Context & Interaction: We pass the SSE session so tools can report progress/logs
            var response = await handler.HandleRequestAsync(request, server, context.User, session, context.RequestAborted);

            // SSE Spec: POST response is 202 Accepted, result is sent via SSE event
            await session.SendResponseAsync(response, context.RequestAborted);

            context.Response.StatusCode = 202; // Accepted
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing SSE message");
            context.Response.StatusCode = 500;
        }
    }
}
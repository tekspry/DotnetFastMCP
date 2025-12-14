using FastMCP.Attributes;
using FastMCP.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using FastMCP.Authentication.Proxy;
using FastMCP.Authentication.Core;
using Microsoft.Extensions.Logging;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Cors.Infrastructure;
using FastMCP.Authentication.McpEndpoints;

namespace FastMCP.Hosting;

/// <summary>
/// A builder for creating and configuring a FastMCP server application.
/// This follows the modern .NET hosting pattern (e.g., WebApplicationBuilder).
/// </summary>
public class McpServerBuilder
{
    private readonly WebApplicationBuilder _webAppBuilder;
    private readonly FastMCPServer _mcpServer;
    private string? _defaultChallengeScheme;

    private McpServerBuilder(FastMCPServer mcpServer, string[]? args)
    {
        _mcpServer = mcpServer;
        _webAppBuilder = WebApplication.CreateBuilder(args ?? Array.Empty<string>());
        _webAppBuilder.Services.AddSingleton(_mcpServer);

        // --- Core Authentication and Authorization Setup ---
        _webAppBuilder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = McpAuthenticationConstants.ApplicationScheme;
            options.DefaultChallengeScheme = "Bearer";  // Default to Bearer for APIs
        })
        .AddCookie(McpAuthenticationConstants.ApplicationScheme);

        _webAppBuilder.Services.AddAuthorization();
        // --- End Core Authentication and Authorization Setup ---

        // --- Rate Limiting Setup ---
        _webAppBuilder.Services.AddRateLimiter(rateLimiterOptions =>
        {
            rateLimiterOptions.AddFixedWindowLimiter("oauth-token", options =>
            {
                options.PermitLimit = 10;
                options.Window = TimeSpan.FromMinutes(1);
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 2;
            });

            rateLimiterOptions.AddFixedWindowLimiter("oauth-register", options =>
            {
                options.PermitLimit = 5;
                options.Window = TimeSpan.FromHours(1);
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 1;
            });

            rateLimiterOptions.AddFixedWindowLimiter("oauth-authorize", options =>
            {
                options.PermitLimit = 30;
                options.Window = TimeSpan.FromMinutes(1);
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = 5;
            });

            rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });
        // --- End Rate Limiting Setup ---

        // --- CORS Setup ---
        _webAppBuilder.Services.AddCors(options =>
        {
            options.AddPolicy("OAuthPolicy", policy =>
            {
                policy
                    .WithOrigins("http://localhost:*", "http://127.0.0.1:*")
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .WithExposedHeaders("WWW-Authenticate", "X-OAuth-Scopes", "Location");
            });
        });
        // --- End CORS Setup ---

        // --- Logging Setup ---
        _webAppBuilder.Services.AddLogging(config =>
        {
            config.AddConsole();
            config.SetMinimumLevel(LogLevel.Information);
            config.AddFilter("FastMCP.Authentication", LogLevel.Debug);
            config.AddFilter("FastMCP.Authentication.Proxy", LogLevel.Debug);
        });
        // --- End Logging Setup ---
    }

    /// <summary>
    /// Creates a new instance of the McpServerBuilder.
    /// </summary>
    public static McpServerBuilder Create(FastMCPServer server, string[]? args = null)
    {
        return new McpServerBuilder(server, args);
    }

    /// <summary>
    /// Scans the specified assembly for methods decorated with McpTool and McpResource 
    /// attributes and registers them with the server.
    /// </summary>
    public McpServerBuilder WithComponentsFrom(Assembly assembly)
    {
        var methods = assembly.GetTypes().SelectMany(t => t.GetMethods());

        foreach (var method in methods)
        {
            if (method.GetCustomAttribute<McpToolAttribute>() is not null)
            {
                _mcpServer.Tools.Add(method);
            }

            if (method.GetCustomAttribute<McpResourceAttribute>() is not null)
            {
                _mcpServer.Resources.Add(method);
            }
        }

        return this;
    }

    /// <summary>
    /// Allows direct configuration of the internal AuthenticationBuilder for advanced scenarios
    /// or adding custom authentication schemes.
    /// </summary>
    public McpServerBuilder WithAuthentication(Action<AuthenticationBuilder> configure)
    {
        configure(_webAppBuilder.Services.AddAuthentication());
        return this;
    }

    /// <summary>
    /// Sets the default challenge scheme for authentication.
    /// </summary>
    public McpServerBuilder WithDefaultChallengeScheme(string schemeName)
    {
        if (string.IsNullOrEmpty(schemeName))
            throw new ArgumentException("Scheme name cannot be null or empty", nameof(schemeName));

        _defaultChallengeScheme = schemeName;

        _webAppBuilder.Services.Configure<AuthenticationOptions>(options =>
        {
            options.DefaultChallengeScheme = schemeName;
        });

        return this;
    }

    /// <summary>
    /// Allows configuration of authorization policies for the MCP server.
    /// </summary>
    public McpServerBuilder WithAuthorization(Action<AuthorizationOptions> configure)
    {
        _webAppBuilder.Services.AddAuthorization(configure);
        return this;
    }

    /// <summary>
    /// Configures CORS policy for OAuth and MCP endpoints.
    /// </summary>
    public McpServerBuilder WithCorsPolicy(string[]? allowedOrigins = null, bool allowCredentials = true)
    {
        _webAppBuilder.Services.Configure<CorsOptions>(options =>
        {
            options.AddPolicy("OAuthPolicy", policy =>
            {
                if (allowedOrigins != null && allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins);
                }
                else
                {
                    policy.SetIsOriginAllowed(_ => true);  // For development only
                }

                policy
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .WithExposedHeaders("WWW-Authenticate", "X-OAuth-Scopes", "Location");

                if (allowCredentials)
                {
                    policy.AllowCredentials();
                }
            });
        });

        return this;
    }

    /// <summary>
    /// Configures OAuth Proxy for providers that don't support DCR.
    /// </summary>
    public McpServerBuilder WithOAuthProxy(
        OAuthProxyOptions options,
        ITokenVerifier tokenVerifier,
        IClientStore? clientStore = null)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));
        if (tokenVerifier == null)
            throw new ArgumentNullException(nameof(tokenVerifier));

        // Register OAuthProxyOptions
        _webAppBuilder.Services.AddSingleton(options);

        // Register ITokenVerifier (if not already registered)
        if (!_webAppBuilder.Services.Any(x => x.ServiceType == typeof(ITokenVerifier)))
        {
            _webAppBuilder.Services.AddSingleton(tokenVerifier);
        }

        // Register IClientStore (ensure it's available)
        if (!_webAppBuilder.Services.Any(x => x.ServiceType == typeof(IClientStore)))
        {
            clientStore ??= new InMemoryClientStore();
            _webAppBuilder.Services.AddSingleton<IClientStore>(clientStore);
        }

        // Register the OAuthProxy service
        var proxy = new OAuthProxy(
            options,
            tokenVerifier,
            clientStore ?? new InMemoryClientStore());

        _webAppBuilder.Services.AddSingleton(proxy);

        // Set default challenge scheme to Bearer for OAuth
        WithDefaultChallengeScheme("Bearer");

        return this;
    }

    /// <summary>
    /// Builds the WebApplication that will host the MCP server.
    /// </summary>
    public WebApplication Build()
    {
        var app = _webAppBuilder.Build();

        // Apply CORS middleware BEFORE authentication
        app.UseCors("OAuthPolicy");

        // Apply rate limiting middleware
        app.UseRateLimiter();

        // CRITICAL: Authentication must run before authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Register OAuth Proxy endpoints if configured
        var oauthProxy = app.Services.GetService<OAuthProxy>();
        if (oauthProxy != null)
        {
            app.MapOAuthProxyEndpoints(oauthProxy);
        }

        // Register the MCP protocol middleware for /mcp endpoints
        app.UseMcpProtocol();

        // Register MCP OAuth discovery endpoints
        var tokenVerifier = app.Services.GetService<ITokenVerifier>();
        app.MapMcpAuthEndpoints(
            mcpPath: "/mcp",
            baseUrl: null,
            tokenVerifier: tokenVerifier);

        // Root endpoint returns server metadata
        app.MapGet("/", () =>
            $"MCP Server '{_mcpServer.Name}' is running.\n" +
            $"Registered Tools: {_mcpServer.Tools.Count}\n" +
            $"Registered Resources: {_mcpServer.Resources.Count}");

        return app;
    }

    // Internal helper to expose the WebApplicationBuilder for extension methods
    internal WebApplicationBuilder GetWebAppBuilder()
    {
        return _webAppBuilder;
    }
}
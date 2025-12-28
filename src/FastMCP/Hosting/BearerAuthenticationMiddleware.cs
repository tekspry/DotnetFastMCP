using FastMCP.Authentication.Core;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Linq; // Add this using directive for LINQ's Select method
using System.Collections.Generic; // Add this for IEnumerable<ITokenVerifier>

namespace FastMCP.Hosting
{
    public class BearerAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public BearerAuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Resolve ALL ITokenVerifier services registered in the DI container
            // This allows multiple token verifiers (e.g., Google, Azure AD) to be used.
            var tokenVerifiers = context.RequestServices.GetServices<ITokenVerifier>(); // <--- KEY CHANGE

            if (tokenVerifiers != null && tokenVerifiers.Any()) // Check if any verifiers are registered
            {
                var authorizationHeader = context.Request.Headers["Authorization"].ToString();

                if (!string.IsNullOrEmpty(authorizationHeader) && authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var token = authorizationHeader.Substring("Bearer ".Length).Trim();
                    AccessToken? verifiedAccessToken = null; // Use nullable AccessToken

                    foreach (var verifier in tokenVerifiers)
                    {
                        try
                        {
                            verifiedAccessToken = await verifier.VerifyTokenAsync(token);
                            if (verifiedAccessToken != null)
                            {
                                // Token successfully verified by one of the verifiers, break loop
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log the exception, but continue to try other verifiers
                            System.Console.WriteLine($"Bearer token verification failed with verifier {verifier.GetType().Name}: {ex.Message}");
                        }
                    }

                    if (verifiedAccessToken != null)
                    {
                        // Convert the IReadOnlyDictionary<string, object> to IEnumerable<Claim>
                        var claims = verifiedAccessToken.Claims.Select(c => new Claim(c.Key, c.Value?.ToString() ?? string.Empty));
                        var claimsIdentity = new ClaimsIdentity(claims, "Bearer");
                        var principal = new ClaimsPrincipal(claimsIdentity);
                        
                        context.User = principal;
                    }
                    else
                    {
                        System.Console.WriteLine("Bearer token verification returned null principal from all configured verifiers.");
                    }
                }
            }
            else
            {
                System.Console.WriteLine("No ITokenVerifier services registered. Bearer token authentication will be skipped.");
            }

            await _next(context);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FastMCP.Authentication.Core;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols; // Add this using directive
using Microsoft.IdentityModel.Protocols.OpenIdConnect; // Add this using directive

namespace FastMCP.Authentication.Verification;

/// <summary>
/// JWT token verifier that validates JWT tokens using JWKS (JSON Web Key Set).
/// This verifier fetches public keys from a JWKS endpoint and validates tokens
/// using standard JWT validation rules (signature, expiration, issuer, audience).
/// </summary>
public class JwtTokenVerifier : ITokenVerifier
{
    private readonly string _jwksUri;
    private readonly string? _issuer;
    private readonly string? _audience;
    private readonly string _algorithm;
    private readonly IReadOnlyList<string> _requiredScopes;
    private readonly ILogger<JwtTokenVerifier>? _logger;
    private readonly HttpClient _httpClient;
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _configurationManager; // ADD THIS

    /// <summary>
    /// Initializes a new instance of the JwtTokenVerifier.
    /// </summary>
    /// <param name="jwksUri">The JWKS endpoint URL to fetch public keys from.</param>
    /// <param name="issuer">The expected issuer claim value (optional).</param>
    /// <param name="audience">The expected audience claim value (optional).</param>
    /// <param name="algorithm">The signing algorithm to validate (default: RS256).</param>
    /// <param name="requiredScopes">Required OAuth scopes for all tokens (optional).</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <param name="httpClient">Optional HttpClient instance. If not provided, a new instance will be created.</param>
    /// <param name="openIdConnectConfigurationUrl">Optional explicit URL for OpenID Connect configuration. If not provided, it is inferred from jwksUri.</param>
    public JwtTokenVerifier(
        string jwksUri,
        string? issuer = null,
        string? audience = null,
        string algorithm = SecurityAlgorithms.RsaSha256,
        IReadOnlyList<string>? requiredScopes = null,
        ILogger<JwtTokenVerifier>? logger = null,
        HttpClient? httpClient = null,
        string? openIdConnectConfigurationUrl = null)
    {
        _jwksUri = jwksUri ?? throw new ArgumentNullException(nameof(jwksUri));
        _issuer = issuer;
        _audience = audience;
        _algorithm = algorithm;
        _requiredScopes = requiredScopes ?? Array.Empty<string>();
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient();

        // Initialize ConfigurationManager to fetch and cache OpenIdConnectConfiguration
        // This implicitly handles fetching the JWKS from the jwksUri
        // If explicit configuration URL is not provided, we infer it from JWKS URI
        var configUrl = openIdConnectConfigurationUrl;
        if (string.IsNullOrEmpty(configUrl))
        {
             configUrl = jwksUri.Replace("/keys", "") + "/.well-known/openid-configuration";
        }

        _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            configUrl,
            new OpenIdConnectConfigurationRetriever(),
            _httpClient);
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> RequiredScopes => _requiredScopes;

    /// <inheritdoc/>
    public async Task<AccessToken?> VerifyTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger?.LogDebug("Token verification failed: token is null or empty");
            return null;
        }

        try
        {
            // Get the OpenIdConnectConfiguration, which contains the signing keys
            var config = await _configurationManager.GetConfigurationAsync(cancellationToken);
            if (config?.SigningKeys == null || !config.SigningKeys.Any())
            {
                _logger?.LogWarning("Failed to retrieve signing keys from OpenIdConnectConfiguration.");
                return null;
            }

            // Configure token validation parameters
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = !string.IsNullOrEmpty(_issuer),
                ValidIssuer = _issuer,
                ValidateAudience = !string.IsNullOrEmpty(_audience),
                ValidAudience = _audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = config.SigningKeys, // USE KEYS FROM CONFIGURATION MANAGER
                ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minutes clock skew
            };

            // ADD THESE DIAGNOSTIC LOGS
            var jwtHandler = new JwtSecurityTokenHandler();
            if (jwtHandler.CanReadToken(token))
            {
                 var jwt = jwtHandler.ReadJwtToken(token);
                 _logger?.LogWarning($"[JwtTokenVerifier] Incoming Token KID: {jwt.Header.Kid}");
                 _logger?.LogWarning($"[JwtTokenVerifier] Incoming Token Issuer: {jwt.Issuer}");
            }

            _logger?.LogDebug($"[JwtTokenVerifier] TokenValidationParameters initialized. ValidateIssuerSigningKey: {validationParameters.ValidateIssuerSigningKey}");
            _logger?.LogDebug($"[JwtTokenVerifier] Number of IssuerSigningKeys set in TokenValidationParameters: {validationParameters.IssuerSigningKeys?.Count() ?? 0}");
            if (validationParameters.IssuerSigningKeys != null)
            {
                foreach (var key in validationParameters.IssuerSigningKeys)
                {
                    _logger?.LogDebug($"[JwtTokenVerifier] Key in validationParameters: Type={key.GetType().Name}, Kid={key.KeyId}");
                }
            }

            // Validate the token
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                _logger?.LogDebug("Token validation failed: token is not a valid JWT");
                return null;
            }

            // Extract scopes from the token
            var scopes = ExtractScopes(jwtToken);

            // Validate required scopes
            if (_requiredScopes.Count > 0)
            {
                var tokenScopes = new HashSet<string>(scopes, StringComparer.OrdinalIgnoreCase);
                var required = new HashSet<string>(_requiredScopes, StringComparer.OrdinalIgnoreCase);
                
                if (!required.IsSubsetOf(tokenScopes))
                {
                    _logger?.LogDebug(
                        "Token missing required scopes. Has: {TokenScopes}, Required: {RequiredScopes}",
                        string.Join(", ", tokenScopes),
                        string.Join(", ", required));
                    return null;
                }
            }

            // Extract expiration
            long? expiresAt = null;
            if (jwtToken.ValidTo != DateTime.MinValue)
            {
                expiresAt = new DateTimeOffset(jwtToken.ValidTo).ToUnixTimeSeconds();
            }

            // Extract claims
            var claims = new Dictionary<string, object>();
            foreach (var claim in jwtToken.Claims)
            {
                // Handle array claims (like scopes)
                if (claims.ContainsKey(claim.Type))
                {
                    if (claims[claim.Type] is List<object> list)
                    {
                        list.Add(claim.Value);
                    }
                    else
                    {
                        var existing = claims[claim.Type];
                        claims[claim.Type] = new List<object> { existing, claim.Value };
                    }
                }
                else
                {
                    claims[claim.Type] = claim.Value;
                }
            }

            // Extract client ID (usually from 'sub', 'client_id', or 'aud' claim)
            var clientId = jwtToken.Subject 
                ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "client_id")?.Value
                ?? jwtToken.Audiences.FirstOrDefault()
                ?? "unknown";

            _logger?.LogDebug("JWT token verified successfully for client {ClientId}", clientId);

            return new AccessToken
            {
                Token = token,
                ClientId = clientId,
                Scopes = scopes,
                ExpiresAt = expiresAt,
                Claims = claims
            };
        }
        catch (SecurityTokenExpiredException)
        {
            _logger?.LogDebug("Token validation failed: token has expired");
            return null;
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger?.LogDebug(ex, "Token validation failed: invalid signature");
            return null;
        }
        catch (SecurityTokenValidationException ex)
        {
            _logger?.LogDebug(ex, "Token validation failed: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unexpected error during token verification");
            return null;
        }
    }

    private IReadOnlyList<string> ExtractScopes(JwtSecurityToken jwtToken)
    {
        // Try to extract scopes from various claim types
        var scopeClaim = jwtToken.Claims.FirstOrDefault(c => 
            c.Type == "scope" || 
            c.Type == "scopes" || 
            c.Type == "http://schemas.microsoft.com/identity/claims/scope");

        if (scopeClaim == null)
            return Array.Empty<string>();

        // Scopes can be space-separated string or array
        var scopeValue = scopeClaim.Value;
        if (string.IsNullOrWhiteSpace(scopeValue))
            return Array.Empty<string>();

        // Handle space-separated scopes
        return scopeValue
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }
}
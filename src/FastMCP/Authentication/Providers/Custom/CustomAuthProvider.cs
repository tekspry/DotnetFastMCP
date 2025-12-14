using FastMCP.Authentication.Core;
using System.Collections.Generic;

namespace FastMCP.Authentication.Providers.Custom;

/// <summary>
/// Custom authentication provider that allows users to implement their own token verification logic.
/// </summary>
public class CustomAuthProvider : IMcpAuthProvider
{
    private readonly ITokenVerifier _tokenVerifier;
    private readonly string? _baseUrl;

    public string? BaseUrl => _baseUrl;

    public string SchemeName => "Custom";

    public IReadOnlyList<string> RequiredScopes => _tokenVerifier.RequiredScopes;

    /// <summary>
    /// Creates a new instance of CustomAuthProvider.
    /// </summary>
    /// <param name="tokenVerifier">The custom token verifier implementation</param>
    /// <param name="baseUrl">Optional base URL for the authentication provider</param>
    public CustomAuthProvider(ITokenVerifier tokenVerifier, string? baseUrl = null)
    {
        _tokenVerifier = tokenVerifier ?? throw new ArgumentNullException(nameof(tokenVerifier));
        _baseUrl = baseUrl;
    }

    /// <summary>
    /// Verifies a token using the custom token verifier.
    /// </summary>
    public async Task<AccessToken?> VerifyTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _tokenVerifier.VerifyTokenAsync(token, cancellationToken);
    }
}
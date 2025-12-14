# Okta OAuth Example

Demonstrates FastMCP server protection with Okta OAuth.

## Setup

### 1. Okta Application Setup

1. Go to [Okta Admin Console](https://admin.okta.com/)
2. Go to **Applications** → **Applications**
3. Click **Create App Integration**
4. Choose **OIDC - OpenID Connect** and **Web Application**
5. Configure:
   - Application name: Your app name
   - Sign-in redirect URIs: `http://localhost:5005/auth/callback`
   - Sign-out redirect URIs: `http://localhost:5005`
6. Note the following values:
   - Okta Domain (e.g., `dev-123456.okta.com`)
   - Client ID
   - Client Secret

### 2. Configuration

Set environment variables:

# Windows PowerShell
$env:FASTMCP_SERVER_AUTH_OKTA_DOMAIN="your-okta-domain" # e.g., dev-123456.okta.com
$env:FASTMCP_SERVER_AUTH_OKTA_CLIENT_ID="your-client-id"
$env:FASTMCP_SERVER_AUTH_OKTA_CLIENT_SECRET="your-client-secret"
$env:FASTMCP_SERVER_AUTH_OKTA_AUDIENCE="api://default" # Optional, often "api://default" for Okta
$env:FASTMCP_SERVER_AUTH_OKTA_REQUIRED_SCOPES="openid,profile,email" # Example scopes

# Linux/Mac
export FASTMCP_SERVER_AUTH_OKTA_DOMAIN="your-okta-domain"
export FASTMCP_SERVER_AUTH_OKTA_CLIENT_ID="your-client-id"
export FASTMCP_SERVER_AUTH_OKTA_CLIENT_SECRET="your-client-secret"
export FASTMCP_SERVER_AUTH_OKTA_AUDIENCE="api://default"
export FASTMCP_SERVER_AUTH_OKTA_REQUIRED_SCOPES="openid,profile,email"

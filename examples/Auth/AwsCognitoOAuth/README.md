# AWS Cognito OAuth Example

Demonstrates FastMCP server protection with AWS Cognito OAuth.

## Setup

### 1. AWS Cognito User Pool Setup

1. Go to [AWS Cognito Console](https://console.aws.amazon.com/cognito/)
2. Create a User Pool or use an existing one.
3. In your User Pool, create an App Client:
   - Configure **Authentication flows**: Enable "Authorization code grant"
   - Under **OAuth 2.0 grant types**: Select "Authorization code grant"
   - Under **Allowed callback URLs**: Add `http://localhost:5003/auth/callback`
   - Under **Allowed OAuth Scopes**: Select scopes your application needs (at minimum: `openid`)
4. Note the following values:
   - User Pool ID (e.g., `us-east-1_XXXXXXXXX`)
   - AWS Region (e.g., `us-east-1`)
   - App Client ID
   - App Client Secret
   - **Important**: You need to configure a domain for your User Pool (e.g., `your-domain-prefix.auth.us-east-1.amazoncognito.com`). This domain prefix will be used in the `FASTMCP_SERVER_AUTH_AWSCOGNITO_DOMAIN` environment variable.

### 2. Configuration

Set environment variables:

# Windows PowerShell
$env:FASTMCP_SERVER_AUTH_AWSCOGNITO_USER_POOL_ID="your-user-pool-id"
$env:FASTMCP_SERVER_AUTH_AWSCOGNITO_AWS_REGION="your-aws-region"
$env:FASTMCP_SERVER_AUTH_AWSCOGNITO_CLIENT_ID="your-app-client-id"
$env:FASTMCP_SERVER_AUTH_AWSCOGNITO_CLIENT_SECRET="your-app-client-secret"
$env:FASTMCP_SERVER_AUTH_AWSCOGNITO_DOMAIN="your-cognito-domain-prefix" # e.g., myapp

# Linux/Mac
export FASTMCP_SERVER_AUTH_AWSCOGNITO_USER_POOL_ID="your-user-pool-id"
export FASTMCP_SERVER_AUTH_AWSCOGNITO_AWS_REGION="your-aws-region"
export FASTMCP_SERVER_AUTH_AWSCOGNITO_CLIENT_ID="your-app-client-id"
export FASTMCP_SERVER_AUTH_AWSCOGNITO_CLIENT_SECRET="your-app-client-secret"
export FASTMCP_SERVER_AUTH_AWSCOGNITO_DOMAIN="your-cognito-domain-prefix"### 3. Run the Server

cd examples/Auth/AwsCognitoOAuth
dotnet runThe server will start on `http://localhost:5003` with:
- MCP endpoint: `http://localhost:5003/mcp`
- OAuth callback: `http://localhost:5003/auth/callback`
- OAuth discovery: `http://localhost:5003/.well-known/oauth-authorization-server`

### 4. Test with MCP Client

MCP clients can automatically discover the authentication requirements and initiate the OAuth flow. The OAuth Proxy handles:
- Dynamic Client Registration (DCR)
- Authorization code flow with PKCE
- Token exchange and validation
# Multi-stage Dockerfile for DotnetFastMCP Server (MCP STDIO & HTTP compliant)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# Copy project files for layer caching
COPY src/FastMCP/FastMCP.csproj src/FastMCP/
COPY examples/BasicServer/BasicServer.csproj examples/BasicServer/

# Restore dependencies
RUN dotnet restore examples/BasicServer/BasicServer.csproj

# Copy remaining source code and publish
COPY src/FastMCP/ src/FastMCP/
COPY examples/BasicServer/ examples/BasicServer/
RUN dotnet publish examples/BasicServer/BasicServer.csproj -c Release -f net8.0 -o /app --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .

# Default to STDIO transport for Model Context Protocol introspection
ENTRYPOINT ["dotnet", "BasicServer.dll", "--stdio"]

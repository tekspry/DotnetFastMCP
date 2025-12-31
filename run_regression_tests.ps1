$ErrorActionPreference = "Stop"

Write-Host "Building Solution..."
dotnet build c:\pocs\FastMCP\DotnetFastMCP\DotnetFastMCP.sln -c Debug

Write-Host "Starting BasicServer..."
$serverProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --project c:\pocs\FastMCP\DotnetFastMCP\examples\BasicServer\BasicServer.csproj --no-build" -PassThru -NoNewWindow
Start-Sleep -Seconds 5

try {
    Write-Host "Running Integration Tests..."
    dotnet run --project c:\pocs\FastMCP\DotnetFastMCP\tests\McpIntegrationTest\McpIntegrationTest.csproj --no-build
}
finally {
    Write-Host "Stopping BasicServer..."
    if ($serverProcess -and !$serverProcess.HasExited) {
        Stop-Process -Id $serverProcess.Id -Force
    }
}

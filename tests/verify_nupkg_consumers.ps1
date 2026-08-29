param(
    [string]$PackageVersion = "2.0.0-test",
    [switch]$SkipCleanup = $false
)

$ErrorActionPreference = "Stop"
$rootDir = (Get-Item $PSScriptRoot).Parent.FullName
Set-Location $rootDir

Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host "  DotnetFastMCP NuGet Package Consumer Verification" -ForegroundColor Cyan
Write-Host "========================================================`n" -ForegroundColor Cyan

# Step 1: Pack the NuGet package locally
Write-Host "[Step 1] Building and packing DotnetFastMCP..." -ForegroundColor Yellow
$nupkgDir = Join-Path $rootDir "nupkgs"
if (Test-Path $nupkgDir) { Remove-Item -Recurse -Force $nupkgDir }
New-Item -ItemType Directory -Path $nupkgDir | Out-Null

dotnet pack src\FastMCP\FastMCP.csproj -c Release -o $nupkgDir /p:Version=$PackageVersion
if ($LASTEXITCODE -ne 0) { throw "dotnet pack failed" }

$packageFile = Get-ChildItem -Path $nupkgDir -Filter "*.nupkg" | Select-Object -First 1
if (-not $packageFile) { throw "No .nupkg file generated in $nupkgDir" }
Write-Host "Generated package: $($packageFile.Name)" -ForegroundColor Green

# Step 2: Inspect package structure
Write-Host "`n[Step 2] Inspecting internal package structure..." -ForegroundColor Yellow
$inspectDir = Join-Path $rootDir "temp_nupkg_inspect"
$tempZip = Join-Path $rootDir "temp_nupkg_inspect.zip"
if (Test-Path $inspectDir) { Remove-Item -Recurse -Force $inspectDir }
if (Test-Path $tempZip) { Remove-Item -Force $tempZip }

Copy-Item $packageFile.FullName $tempZip
Expand-Archive -Path $tempZip -DestinationPath $inspectDir -Force
Remove-Item $tempZip -Force

$libFolders = Get-ChildItem (Join-Path $inspectDir "lib") | Select-Object -ExpandProperty Name
Write-Host "Found Target Framework Folders in Package: $($libFolders -join ', ')" -ForegroundColor Cyan

# Step 3: Test consumption in sandbox applications
$sandboxesDir = Join-Path $rootDir "temp_consumer_sandboxes"
if (Test-Path $sandboxesDir) { Remove-Item -Recurse -Force $sandboxesDir }
New-Item -ItemType Directory -Path $sandboxesDir | Out-Null

$tfmsToTest = @("net8.0")
# Check if .NET 10 is available
$sdks = dotnet --list-sdks
if ($sdks -match "10\.") {
    $tfmsToTest += "net10.0"
}

foreach ($tfm in $tfmsToTest) {
    Write-Host "`n[Step 3] Testing consumer sandbox app targeting [$tfm]..." -ForegroundColor Yellow
    $appDir = Join-Path $sandboxesDir "App_$($tfm.Replace('.', '_'))"
    New-Item -ItemType Directory -Path $appDir | Out-Null
    
    # Create console app
    dotnet new console -o $appDir -f $tfm --no-restore
    
    # Add local NuGet source and reference package
    $csprojPath = Join-Path $appDir "App_$($tfm.Replace('.', '_')).csproj"
    dotnet add $csprojPath package DotnetFastMCP --version $PackageVersion --source $nupkgDir
    
    # Write a simple MCP test program
    $programCs = @"
using FastMCP.Server;
using FastMCP.Attributes;
using FastMCP.Hosting;

var server = new FastMCPServer("ConsumerTestServer");
Console.WriteLine("Successfully created FastMCPServer on " + Environment.Version);
"@
    Set-Content -Path (Join-Path $appDir "Program.cs") -Value $programCs
    
    # Build and run
    Write-Host "Building and running on [$tfm]..." -ForegroundColor Gray
    dotnet run --project $csprojPath
    if ($LASTEXITCODE -ne 0) { throw "Consumer test failed for $tfm" }
    
    Write-Host "Consumer test passed for [$tfm]!" -ForegroundColor Green
}

# Cleanup
if (-not $SkipCleanup) {
    Write-Host "`nCleaning up test sandboxes..." -ForegroundColor Gray
    Remove-Item -Recurse -Force $inspectDir -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force $sandboxesDir -ErrorAction SilentlyContinue
}

Write-Host "`nAll Consumer Contract Tests Passed Successfully!`n" -ForegroundColor Green

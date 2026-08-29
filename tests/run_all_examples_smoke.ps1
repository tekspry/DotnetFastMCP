param(
    [switch]$BuildOnly = $false
)

$ErrorActionPreference = "Stop"
$rootDir = (Get-Item $PSScriptRoot).Parent.FullName

Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host "  DotnetFastMCP Examples Build & Smoke Test Harness" -ForegroundColor Cyan
Write-Host "========================================================`n" -ForegroundColor Cyan

$exampleProjects = Get-ChildItem -Path (Join-Path $rootDir "examples") -Recurse -Filter "*.csproj"

Write-Host "Found $($exampleProjects.Count) example projects to verify:`n" -ForegroundColor Cyan

$results = @()

foreach ($proj in $exampleProjects) {
    $projName = $proj.Name.Replace(".csproj", "")
    Write-Host "Building: $projName ($($proj.FullName))..." -ForegroundColor Yellow -NoNewline
    
    $buildOutput = dotnet build $proj.FullName -c Release 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host " [PASS]" -ForegroundColor Green
        $results += [PSCustomObject]@{ Project = $projName; Status = "PASS"; Error = "" }
    } else {
        Write-Host " [FAIL]" -ForegroundColor Red
        $results += [PSCustomObject]@{ Project = $projName; Status = "FAIL"; Error = ($buildOutput | Out-String) }
    }
}

Write-Host "`n========================================================" -ForegroundColor Cyan
Write-Host "  Summary Results" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
$results | Format-Table -AutoSize

$failed = $results | Where-Object { $_.Status -eq "FAIL" }
if ($failed.Count -gt 0) {
    throw "$($failed.Count) example project(s) failed to build!"
} else {
    Write-Host "🎉 All $($exampleProjects.Count) example projects compiled successfully!" -ForegroundColor Green
}

#! /usr/bin/env pwsh
# Integration test script for BasicServer

Write-Host "=== DotnetFastMCP BasicServer Test ===" -ForegroundColor Cyan
Write-Host ""

# Give the server a moment to start
Write-Host "Waiting for server to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

# Test 1: Check root endpoint
Write-Host "Test 1: Testing root endpoint (GET http://localhost:5000/)" -ForegroundColor Green
try {
    $rootResponse = Invoke-RestMethod -Uri http://localhost:5000/ -Method GET -ErrorAction Stop
    Write-Host "✓ Success!" -ForegroundColor Green
    Write-Host "Response:" -ForegroundColor Cyan
    Write-Host $rootResponse
    Write-Host ""
} catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
    Write-Host ""
}

# Test 2: Add tool with array parameters
Write-Host "Test 2: Testing Add tool with array parameters" -ForegroundColor Green
$jsonRequest2 = '{"jsonrpc": "2.0", "method": "Add", "params": [5, 3], "id": 1}'
Write-Host "Request: $jsonRequest2" -ForegroundColor Yellow
try {
    $addRequest = $jsonRequest2
    
    $addResponse = Invoke-RestMethod -Uri http://localhost:5000/mcp -Method POST -ContentType "application/json" -Body $addRequest -ErrorAction Stop
    Write-Host "✓ Success!" -ForegroundColor Green
    Write-Host "Response:" -ForegroundColor Cyan
    Write-Host ($addResponse | ConvertTo-Json)
    Write-Host ""
} catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
    Write-Host ""
}

# Test 3: Add tool with named parameters
Write-Host "Test 3: Testing Add tool with named parameters" -ForegroundColor Green
$jsonRequest3 = '{"jsonrpc": "2.0", "method": "Add", "params": {"a": 10, "b": 20}, "id": 2}'
Write-Host "Request: $jsonRequest3" -ForegroundColor Yellow
try {
    $addNamedRequest = $jsonRequest3
    
    $addNamedResponse = Invoke-RestMethod -Uri http://localhost:5000/mcp -Method POST -ContentType "application/json" -Body $addNamedRequest -ErrorAction Stop
    Write-Host "✓ Success!" -ForegroundColor Green
    Write-Host "Response:" -ForegroundColor Cyan
    Write-Host ($addNamedResponse | ConvertTo-Json)
    Write-Host ""
} catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
    Write-Host ""
}

# Test 4: Test method not found error
Write-Host "Test 4: Testing error handling (method not found)" -ForegroundColor Green
$jsonRequest4 = '{"jsonrpc": "2.0", "method": "NonExistentMethod", "params": [], "id": 3}'
Write-Host "Request: $jsonRequest4" -ForegroundColor Yellow
try {
    $errorRequest = $jsonRequest4
    
    $errorResponse = Invoke-RestMethod -Uri http://localhost:5000/mcp -Method POST -ContentType "application/json" -Body $errorRequest -ErrorAction Stop
    Write-Host "✓ Success (got expected error response)!" -ForegroundColor Green
    Write-Host "Response:" -ForegroundColor Cyan
    Write-Host ($errorResponse | ConvertTo-Json)
    Write-Host ""
} catch {
    Write-Host "✗ Failed: $_" -ForegroundColor Red
    Write-Host ""
}

Write-Host "=== All tests completed ===" -ForegroundColor Cyan

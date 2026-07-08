# SunyaSuite — Start API + WASM Client
Write-Host "=== Starting SunyaSuite ===" -ForegroundColor Cyan

$root = Split-Path -Parent $MyInvocation.MyCommand.Path

# Start API in background
Write-Host "[API] Starting on http://localhost:5000 ..." -ForegroundColor Green
$apiJob = Start-Job -ScriptBlock {
    Set-Location $using:root
    dotnet run --project src/SunyaSuite.Web --launch-profile http
}

# Wait for API to start
Start-Sleep -Seconds 3

# Start WASM client
Write-Host "[WASM] Starting on http://localhost:5002 ..." -ForegroundColor Green
$clientJob = Start-Job -ScriptBlock {
    Set-Location $using:root
    dotnet run --project src/SunyaSuite.Web.Client --launch-profile http
}

Write-Host ""
Write-Host "  API  → http://localhost:5000" -ForegroundColor Yellow
Write-Host "  App  → http://localhost:5002" -ForegroundColor Yellow
Write-Host "  Swagger → http://localhost:5000/openapi/v1.json" -ForegroundColor Yellow
Write-Host ""
Write-Host "Press any key to stop all services..." -ForegroundColor Cyan

$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

Write-Host "Stopping..." -ForegroundColor Red
Stop-Job $apiJob
Stop-Job $clientJob
Remove-Job $apiJob
Remove-Job $clientJob

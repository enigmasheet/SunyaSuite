param()

Write-Host "=== EF Core Migration Creator ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "Select DbContext:" -ForegroundColor Yellow
Write-Host "  1) ConfigDbContext (Identity, Organizations)"
Write-Host "  2) ApplicationDbContext (Tenant: Clients, Invoices, etc.)"
$choice = Read-Host "Enter 1 or 2"

switch ($choice) {
    "1" {
        $contextName = "ConfigDbContext"
        $outputDir = "Data\Migrations\ConfigDb"
    }
    "2" {
        $contextName = "ApplicationDbContext"
        $outputDir = "Data\Tenant\Migrations"
    }
    default {
        Write-Host "Invalid choice. Exiting." -ForegroundColor Red
        exit 1
    }
}

$migrationName = Read-Host "Enter migration name"
if ([string]::IsNullOrWhiteSpace($migrationName)) {
    Write-Host "Migration name cannot be empty. Exiting." -ForegroundColor Red
    exit 1
}

$project = "src/SunyaSuite.Infrastructure"
$startupProject = "src/SunyaSuite.Web"

Write-Host ""
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "DbContext:       $contextName"
Write-Host "Migration:       $migrationName"
Write-Host "Output Dir:      $outputDir"
Write-Host "Project:         $project"
Write-Host "Startup Project: $startupProject"
$confirm = Read-Host "Proceed? (Y/n)"
if ($confirm -eq "n" -or $confirm -eq "N") {
    Write-Host "Cancelled." -ForegroundColor Yellow
    exit 0
}

$cmd = "dotnet ef migrations add $migrationName --project $project --startup-project $startupProject --context $contextName --output-dir `"$outputDir`""
Write-Host ""
Write-Host "Running: $cmd" -ForegroundColor Green
Invoke-Expression $cmd

if ($LASTEXITCODE -eq 0) {
    Write-Host "Migration '$migrationName' created successfully." -ForegroundColor Green
} else {
    Write-Host "Migration creation failed." -ForegroundColor Red
    exit 1
}

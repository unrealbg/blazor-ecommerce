param(
    [ValidateSet('Development', 'Staging', 'Testing')]
    [string]$Environment = 'Development',
    [ValidateSet('none', 'minimal', 'demo', 'test')]
    [string]$SeedMode = 'minimal',
    [switch]$UseCompose
)

$ErrorActionPreference = 'Stop'

$env:ASPNETCORE_ENVIRONMENT = $Environment
$env:Release__SeedMode = $SeedMode
$env:Release__MigrationMode = 'manual'

Write-Host "Environment: $Environment"
Write-Host "Seed mode:   $SeedMode"

if ($UseCompose) {
    docker compose up --build
    exit $LASTEXITCODE
}

Write-Host 'Starting AppHost and Storefront with current profile. Use separate terminals if you want them detached.'
Write-Host '1. dotnet run --project src/AppHost/AppHost.csproj'
Write-Host '2. dotnet run --project src/Storefront/Storefront.Web/Storefront.Web.csproj'
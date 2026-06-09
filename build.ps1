param(
    [string]$Configuration = "Release",
    [string]$Rid = ""
)

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

Write-Host "=== LocalProxy Build ==="
Write-Host "Configuration: $Configuration"

# Restore
Write-Host "`n[1/3] Restoring packages..."
dotnet restore

# Build
Write-Host "`n[2/3] Building..."
dotnet build -c $Configuration --no-restore

# Publish CLI (AOT)
Write-Host "`n[3/3] Publishing CLI (AOT)..."
if ($Rid) {
    dotnet publish src/LocalProxy.Cli/LocalProxy.Cli.csproj `
        -c $Configuration `
        -r $Rid `
        --self-contained `
        -o artifacts/cli/$Rid
    Write-Host "CLI published to artifacts/cli/$Rid/"
} else {
    dotnet publish src/LocalProxy.Cli/LocalProxy.Cli.csproj `
        -c $Configuration `
        -o artifacts/cli/
    Write-Host "CLI published to artifacts/cli/"
}

Write-Host "`n=== Build complete ==="

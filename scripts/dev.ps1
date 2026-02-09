# 🧪 Horizon Platform - Unified Dev Helper

# This is the master entry point for the "Implementation Excellence" pillar.
# It wraps start-docker.ps1 to provide a single, compliant command for developers.

param (
    [switch]$SkipBuild,
    [switch]$Setup
)

$ScriptFolder = Split-Path -Parent $MyInvocation.MyCommand.Definition

# 1. Run Setup if requested
if ($Setup) {
    Write-Host "🚀 Running Setup..." -ForegroundColor Cyan
    & "$ScriptFolder/scripts/setup-dev.ps1"
}

# 2. Load Environment Variables
# We need to load .env manually so docker-compose down has access to variables
$EnvPath = "$ScriptFolder/.env"
if (Test-Path $EnvPath) {
    Get-Content $EnvPath | ForEach-Object {
        $line = $_.Trim()
        if ($line -notmatch '^#' -and $line -match '=') {
            $name, $value = $line.Split('=', 2)
            [System.Environment]::SetEnvironmentVariable($name, $value)
        }
    }
}

# 3. Self-Healing Environment
# Clear out zombie services and orphan containers to ensure a clean state
Write-Host "🩹 Self-Healing: Cleaning up zombie services..." -ForegroundColor Gray
docker-compose down --remove-orphans

# 3. Run Docker initialization
Write-Host "🐳 Launching Horizon Infrastructure..." -ForegroundColor Cyan
$Params = @{}
if ($SkipBuild) { $Params["SkipBuild"] = $true }

& "$ScriptFolder/scripts/start-docker.ps1" @Params

Write-Host "🏁 Development environment ready." -ForegroundColor Green

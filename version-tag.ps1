# Automatic version tagging script for PowerShell
# Usage: .\version-tag.ps1 [patch|minor|major] [message]

param(
    [Parameter(Position=0)]
    [ValidateSet("patch", "minor", "major")]
    [string]$VersionType = "patch",
    
    [Parameter(Position=1)]
    [string]$Message = "Auto version bump"
)

# Get current version tag
$currentTag = git describe --tags --abbrev=0 2>$null
if (-not $currentTag) {
    $currentTag = "v0.0.0"
}

# Extract version numbers
$versionString = $currentTag -replace '^v', ''
$versionParts = $versionString -split '\.'
$major = [int]($versionParts[0] ?? 0)
$minor = [int]($versionParts[1] ?? 0)
$patch = [int]($versionParts[2] ?? 0)

# Bump version based on type
switch ($VersionType) {
    "major" {
        $major++
        $minor = 0
        $patch = 0
    }
    "minor" {
        $minor++
        $patch = 0
    }
    "patch" {
        $patch++
    }
}

$newVersion = "v$major.$minor.$patch"

# Create tag
git tag -a $newVersion -m $Message

Write-Host "✅ Created tag: $newVersion" -ForegroundColor Green
Write-Host "To push: git push origin $newVersion" -ForegroundColor Yellow

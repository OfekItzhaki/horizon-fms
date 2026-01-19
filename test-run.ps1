Write-Host "================================" -ForegroundColor Cyan
Write-Host "RUNNING APPLICATION..." -ForegroundColor Green
Write-Host "Watch for errors below..." -ForegroundColor Yellow
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

dotnet run --project FileManagementSystem.Presentation/FileManagementSystem.Presentation.csproj

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Application closed." -ForegroundColor Yellow
Write-Host "Check the output above for any errors." -ForegroundColor Yellow

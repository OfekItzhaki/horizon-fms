Write-Host "Starting File Management System..." -ForegroundColor Green
Write-Host "Errors will appear below:" -ForegroundColor Yellow
Write-Host "================================" -ForegroundColor Yellow
Write-Host ""

# Change to the project directory
Set-Location "FileManagementSystem.Presentation"

# Run the application and capture output
dotnet run 2>&1 | Tee-Object -Variable output

Write-Host ""
Write-Host "================================" -ForegroundColor Yellow
Write-Host "If you see errors above, copy them and share them." -ForegroundColor Yellow
Write-Host ""
Write-Host "Also check the log file at:" -ForegroundColor Cyan
Write-Host "  logs\filemanager.log" -ForegroundColor Cyan

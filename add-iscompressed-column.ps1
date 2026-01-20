# Script to add IsCompressed column to FileItems table
# This can be run while the API is running (SQLite supports concurrent reads)

$dbPath = "FileManagementSystem.API\filemanager.db"
$sql = "ALTER TABLE FileItems ADD COLUMN IsCompressed INTEGER NOT NULL DEFAULT 1;"

Write-Host "Adding IsCompressed column to FileItems table..." -ForegroundColor Cyan

try {
    Add-Type -Path (Get-Item "FileManagementSystem.API\bin\Debug\net8.0\Microsoft.Data.Sqlite.dll").FullName -ErrorAction SilentlyContinue
    
    $connectionString = "Data Source=$dbPath"
    $connection = New-Object Microsoft.Data.Sqlite.SqliteConnection($connectionString)
    $connection.Open()
    
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $command.ExecuteNonQuery()
    
    $connection.Close()
    
    Write-Host "✅ Successfully added IsCompressed column!" -ForegroundColor Green
    Write-Host "All existing files will have IsCompressed = 1 (true)" -ForegroundColor Yellow
}
catch {
    Write-Host "❌ Error: $_" -ForegroundColor Red
    Write-Host "Make sure the database file exists and is not locked." -ForegroundColor Yellow
    exit 1
}

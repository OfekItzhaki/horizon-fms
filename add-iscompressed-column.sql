-- SQL script to add IsCompressed column to FileItems table
-- Run this using any SQLite tool (DB Browser for SQLite, sqlite3 CLI, etc.)
-- Or use: dotnet tool install -g dotnet-script
-- Then run: dotnet script add-iscompressed-column.csx

ALTER TABLE FileItems ADD COLUMN IsCompressed INTEGER NOT NULL DEFAULT 1;

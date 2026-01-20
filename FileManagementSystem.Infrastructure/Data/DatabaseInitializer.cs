using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using FileManagementSystem.Domain.Entities;

namespace FileManagementSystem.Infrastructure.Data;

public class DatabaseInitializer
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(AppDbContext dbContext, ILogger<DatabaseInitializer> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        if (_dbContext.Database.EnsureCreated())
        {
            _logger.LogInformation("Database created successfully");
            await SeedData.SeedAsync(_dbContext);
            _logger.LogInformation("Database seeded with initial data");
        }
        else
        {
            await ApplyMigrationsAsync();
            await EnsureSchemaColumnsAsync();
            await SeedIfNeededAsync();
            await RenameCFolderToDefaultAsync();
        }
    }

    private async Task ApplyMigrationsAsync()
    {
        try
        {
            await _dbContext.Database.MigrateAsync();
            _logger.LogInformation("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No migrations found or migration failed, using existing database");
        }
    }

    private async Task EnsureSchemaColumnsAsync()
    {
        try
        {
            var connection = _dbContext.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            try
            {
                await EnsureIsCompressedColumnAsync(connection);
                await EnsureFileNameColumnAsync(connection);
                await PopulateFileNameValuesAsync(connection);
            }
            finally
            {
                if (connection.State == System.Data.ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not add columns automatically. You may need to run manual migrations");
        }
    }

    private async Task EnsureIsCompressedColumnAsync(DbConnection connection)
    {
        var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = "SELECT COUNT(*) FROM pragma_table_info('FileItems') WHERE name='IsCompressed'";
        var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

        if (!exists)
        {
            var addCommand = connection.CreateCommand();
            addCommand.CommandText = "ALTER TABLE FileItems ADD COLUMN IsCompressed INTEGER NOT NULL DEFAULT 1";
            await addCommand.ExecuteNonQueryAsync();
            _logger.LogInformation("Added IsCompressed column to FileItems table");
        }
    }

    private async Task EnsureFileNameColumnAsync(DbConnection connection)
    {
        var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = "SELECT COUNT(*) FROM pragma_table_info('FileItems') WHERE name='FileName'";
        var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

        if (!exists)
        {
            var addCommand = connection.CreateCommand();
            addCommand.CommandText = "ALTER TABLE FileItems ADD COLUMN FileName TEXT";
            await addCommand.ExecuteNonQueryAsync();
            _logger.LogInformation("Added FileName column to FileItems table");
        }
    }

    private async Task PopulateFileNameValuesAsync(DbConnection connection)
    {
        try
        {
            // Update NULL FileName values to empty string
            var updateCommand = connection.CreateCommand();
            updateCommand.CommandText = @"
                UPDATE FileItems 
                SET FileName = '' 
                WHERE FileName IS NULL";
            var nullCount = await updateCommand.ExecuteNonQueryAsync();

            if (nullCount > 0)
            {
                _logger.LogInformation("Set {Count} NULL FileName values to empty string", nullCount);
            }

            // Populate empty FileName values from Path
            var selectCommand = connection.CreateCommand();
            selectCommand.CommandText = @"
                SELECT Id, Path 
                FROM FileItems 
                WHERE (FileName IS NULL OR FileName = '') AND Path IS NOT NULL AND Path != ''";

            using var reader = await selectCommand.ExecuteReaderAsync();
            var updateCommands = new List<(Guid Id, string FileName)>();

            while (await reader.ReadAsync())
            {
                var fileId = reader.GetGuid(0);
                var path = reader.GetString(1);
                var fileName = System.IO.Path.GetFileName(path);

                if (!string.IsNullOrEmpty(fileName))
                {
                    updateCommands.Add((fileId, fileName));
                }
            }

            // Update each file with its extracted filename
            foreach (var (id, fileName) in updateCommands)
            {
                var updateCmd = connection.CreateCommand();
                updateCmd.CommandText = "UPDATE FileItems SET FileName = @fileName WHERE Id = @id";
                var param1 = updateCmd.CreateParameter();
                param1.ParameterName = "@fileName";
                param1.Value = fileName;
                updateCmd.Parameters.Add(param1);
                var param2 = updateCmd.CreateParameter();
                param2.ParameterName = "@id";
                param2.Value = id;
                updateCmd.Parameters.Add(param2);
                await updateCmd.ExecuteNonQueryAsync();
            }

            if (updateCommands.Count > 0)
            {
                _logger.LogInformation("Populated FileName for {Count} files from Path using C# extraction", updateCommands.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error updating FileName values");
        }
    }

    private async Task SeedIfNeededAsync()
    {
        if (!_dbContext.Set<User>().Any())
        {
            await SeedData.SeedAsync(_dbContext);
            _logger.LogInformation("Database seeded with initial data");
        }
    }

    private async Task RenameCFolderToDefaultAsync()
    {
        try
        {
            var storageBasePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FileManagementSystem",
                "Storage"
            );
            var defaultFolderPath = System.IO.Path.Combine(storageBasePath, "Default");

            // Ensure Default folder exists
            var defaultFolder = await _dbContext.Set<Folder>()
                .FirstOrDefaultAsync(f => f.Path == defaultFolderPath || f.Name == "Default");

            if (defaultFolder == null)
            {
                // Create Default folder if it doesn't exist
                defaultFolder = new Folder
                {
                    Name = "Default",
                    Path = defaultFolderPath,
                    ParentFolderId = null,
                    CreatedDate = DateTime.UtcNow
                };
                _dbContext.Set<Folder>().Add(defaultFolder);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Created Default folder: {Path}", defaultFolderPath);
            }

            // Rename any C: folder to Default or move its files
            var cFolder = await _dbContext.Set<Folder>()
                .FirstOrDefaultAsync(f => f.Name == "C:" || f.Path == "C:" || f.Path == "C:\\");

            if (cFolder != null)
            {
                if (cFolder.Id == defaultFolder.Id)
                {
                    // Already the same folder, just update name/path if needed
                    if (cFolder.Name != "Default" || cFolder.Path != defaultFolderPath)
                    {
                        cFolder.Name = "Default";
                        cFolder.Path = defaultFolderPath;
                        await _dbContext.SaveChangesAsync();
                        _logger.LogInformation("Updated C: folder to Default folder");
                    }
                }
                else
                {
                    // Move files from C: to Default and delete C: folder
                    var filesInCFolder = await _dbContext.Set<FileItem>()
                        .Where(f => f.FolderId == cFolder.Id)
                        .ToListAsync();

                    foreach (var file in filesInCFolder)
                    {
                        file.FolderId = defaultFolder.Id;
                    }

                    _dbContext.Set<Folder>().Remove(cFolder);
                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("Moved {Count} files from C: folder to Default folder and deleted C: folder", filesInCFolder.Count);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not ensure Default folder exists");
        }
    }
}

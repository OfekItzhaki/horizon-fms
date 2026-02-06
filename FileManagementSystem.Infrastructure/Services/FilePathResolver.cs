using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using FileManagementSystem.Application.Interfaces;

namespace FileManagementSystem.Infrastructure.Services;

public class FilePathResolver : IFilePathResolver
{
    private readonly ILogger<FilePathResolver> _logger;

    public FilePathResolver(ILogger<FilePathResolver> logger)
    {
        _logger = logger;
    }

    public string? ResolveFilePath(string storedPath, bool isCompressed)
    {
        var triedPaths = new List<string>();
        var pathsToTry = new List<string>();

        // Helper to add both compressed and uncompressed versions
        void AddPathVariations(string basePath)
        {
            if (string.IsNullOrEmpty(basePath)) return;

            pathsToTry.Add(basePath);

            if (!basePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
            {
                pathsToTry.Add(basePath + ".gz");
            }
        }

        // 1. The stored path as-is (try both compressed and uncompressed)
        AddPathVariations(storedPath);

        // 2. If stored path is relative, try resolving it
        if (!Path.IsPathRooted(storedPath))
        {
            var storageBasePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FileManagementSystem",
                "Storage"
            );
            var resolvedPath = Path.GetFullPath(Path.Combine(storageBasePath, storedPath.TrimStart('\\', '/')));
            AddPathVariations(resolvedPath);

            try
            {
                var workingDirPath = Path.GetFullPath(storedPath);
                if (workingDirPath != storedPath)
                {
                    AddPathVariations(workingDirPath);
                }
            }
            catch
            {
                // Ignore if path is invalid
            }
        }

        // Try each path until we find the file
        foreach (var pathToTry in pathsToTry.Distinct())
        {
            triedPaths.Add(pathToTry);
            var exists = System.IO.File.Exists(pathToTry);
            _logger.LogInformation("Checking path: {Path} | Exists: {Exists}", pathToTry, exists);
            
            if (exists)
            {
                _logger.LogInformation("Found file at: {FilePath}", pathToTry);
                return pathToTry;
            }
        }

        // If still not found and path is absolute, try to list the directory
        if (Path.IsPathRooted(storedPath))
        {
            try
            {
                var directory = Path.GetDirectoryName(storedPath);
                if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                {
                    var filesInDir = Directory.GetFiles(directory);
                    _logger.LogWarning("Directory exists but file not found. Files in directory: {Files}", 
                        string.Join(", ", filesInDir.Select(Path.GetFileName)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not list directory contents");
            }
        }

        // Log all tried paths for debugging
        _logger.LogError("File not found. Stored path: {StoredPath}, IsCompressed: {IsCompressed}, Tried {Count} paths:", 
            storedPath, isCompressed, triedPaths.Count);
        foreach (var triedPath in triedPaths)
        {
            var exists = File.Exists(triedPath);
            _logger.LogError("  Path: {Path} | Exists: {Exists}", triedPath, exists);
        }

        return null;
    }
}

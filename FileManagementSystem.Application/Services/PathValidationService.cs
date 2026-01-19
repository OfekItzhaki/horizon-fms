using System.IO;
using Microsoft.Extensions.Logging;

namespace FileManagementSystem.Application.Services;

public class PathValidationService
{
    private readonly ILogger<PathValidationService> _logger;
    private readonly List<string> _allowedBasePaths;
    
    public PathValidationService(ILogger<PathValidationService> logger)
    {
        _logger = logger;
        _allowedBasePaths = new List<string>
        {
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };
    }
    
    public bool IsPathValid(string path, out string normalizedPath)
    {
        normalizedPath = string.Empty;
        
        try
        {
            // Normalize the path
            var normalized = Path.GetFullPath(path);
            normalizedPath = normalized;
            
            // Check for path traversal attempts
            if (path.Contains("..") || path.Contains("~"))
            {
                _logger.LogWarning("Path traversal attempt detected: {Path}", path);
                return false;
            }
            
            // Check if path is within allowed base paths
            var isAllowed = _allowedBasePaths.Any(basePath => 
                normalized.StartsWith(basePath, StringComparison.OrdinalIgnoreCase));
            
            if (!isAllowed)
            {
                _logger.LogWarning("Path outside allowed directories: {Path}", normalizedPath);
                return false;
            }
            
            // Check for invalid characters
            if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                _logger.LogWarning("Path contains invalid characters: {Path}", path);
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating path: {Path}", path);
            return false;
        }
    }
    
    public bool IsFileNameValid(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }
        
        // Check for invalid characters
        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            return false;
        }
        
        // Check for reserved names (Windows)
        var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant();
        
        if (reservedNames.Contains(fileNameWithoutExt))
        {
            return false;
        }
        
        return true;
    }
    
    public void AddAllowedBasePath(string path)
    {
        var normalized = Path.GetFullPath(path);
        if (!_allowedBasePaths.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            _allowedBasePaths.Add(normalized);
        }
    }
}

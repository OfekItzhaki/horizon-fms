using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using FileManagementSystem.Application.Interfaces;

namespace FileManagementSystem.Infrastructure.Services;

public class StorageService : IStorageService
{
    private readonly ILogger<StorageService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _thumbnailDirectory;
    
    public StorageService(ILogger<StorageService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        var configDir = _configuration["ThumbnailSettings:Directory"];
        _thumbnailDirectory = !string.IsNullOrEmpty(configDir)
            ? configDir
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "FileManagementSystem", "Thumbnails");
        
        Directory.CreateDirectory(_thumbnailDirectory);
    }
    
    public async Task<string> SaveFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default)
    {
        var destinationDir = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(destinationDir))
        {
            Directory.CreateDirectory(destinationDir);
        }
        
        // Ensure destination path is unique if file exists
        var finalPath = destinationPath;
        var counter = 1;
        while (File.Exists(finalPath))
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(destinationPath);
            var extension = Path.GetExtension(destinationPath);
            var directory = Path.GetDirectoryName(destinationPath);
            finalPath = Path.Combine(directory ?? "", $"{fileNameWithoutExtension}_{counter}{extension}");
            counter++;
        }
        
        // Use async file operations
        await using var sourceStream = File.OpenRead(sourcePath);
        await using var destinationStream = File.Create(finalPath);
        await sourceStream.CopyToAsync(destinationStream, cancellationToken);
        return finalPath;
    }
    
    public async Task<bool> DeleteFileAsync(string filePath, bool moveToRecycleBin = true, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return false;
        }
        
        try
        {
            if (moveToRecycleBin)
            {
                // On Windows, use shell32 to move to recycle bin
                await Task.Run(() =>
                {
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                        filePath,
                        Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                        Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                }, cancellationToken);
            }
            else
            {
                await Task.Run(() => File.Delete(filePath), cancellationToken);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
            return false;
        }
    }
    
    public async Task<string> GenerateThumbnailAsync(string imagePath, int maxWidth, int maxHeight, CancellationToken cancellationToken = default)
    {
        try
        {
            var fileHash = await ComputeHashAsync(imagePath, cancellationToken);
            var hashHex = Convert.ToHexString(fileHash);
            var thumbnailPath = Path.Combine(_thumbnailDirectory, $"{hashHex}.jpg");
            
            if (File.Exists(thumbnailPath))
            {
                return thumbnailPath;
            }
            
            await using var imageStream = File.OpenRead(imagePath);
            using var image = await Image.LoadAsync(imageStream, cancellationToken);
            
            var size = image.Size;
            if (size.Width > maxWidth || size.Height > maxHeight)
            {
                var ratio = Math.Min((double)maxWidth / size.Width, (double)maxHeight / size.Height);
                var newWidth = (int)(size.Width * ratio);
                var newHeight = (int)(size.Height * ratio);
                
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(newWidth, newHeight),
                    Mode = ResizeMode.Max
                }));
            }
            
            await using var outputStream = File.Create(thumbnailPath);
            await image.SaveAsync(outputStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder(), cancellationToken);
            
            return thumbnailPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail for: {ImagePath}", imagePath);
            throw;
        }
    }
    
    public async Task<byte[]> ComputeHashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        using var sha256 = SHA256.Create();
        await using var fileStream = File.OpenRead(filePath);
        
        return await sha256.ComputeHashAsync(fileStream, cancellationToken);
    }
}

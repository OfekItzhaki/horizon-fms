using System.Security.Cryptography;
using System.IO.Compression;
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
        
        // Add .gz extension for compressed files
        var compressedPath = destinationPath + ".gz";
        
        // Ensure destination path is unique if file exists
        var finalPath = compressedPath;
        var counter = 1;
        while (File.Exists(finalPath))
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(destinationPath);
            var extension = Path.GetExtension(destinationPath);
            var directory = Path.GetDirectoryName(destinationPath);
            finalPath = Path.Combine(directory ?? "", $"{fileNameWithoutExtension}_{counter}{extension}.gz");
            counter++;
        }
        
        // Read source file and compress it
        var sourceFileInfo = new FileInfo(sourcePath);
        var originalSize = sourceFileInfo.Length;
        
        await using var sourceStream = File.OpenRead(sourcePath);
        await using var compressedStream = File.Create(finalPath);
        await using var gzipStream = new GZipStream(compressedStream, CompressionLevel.Optimal);
        
        await sourceStream.CopyToAsync(gzipStream, cancellationToken);
        await gzipStream.FlushAsync(cancellationToken);
        
        var compressedSize = new FileInfo(finalPath).Length;
        var compressionRatio = originalSize > 0 ? (1.0 - (double)compressedSize / originalSize) * 100 : 0;
        
        _logger.LogInformation("File compressed: {SourcePath} -> {CompressedPath}, Original: {OriginalSize} bytes, Compressed: {CompressedSize} bytes, Ratio: {CompressionRatio:F2}%",
            sourcePath, finalPath, originalSize, compressedSize, compressionRatio);
        
        return finalPath;
    }
    
    public async Task<FileData> ReadFileAsync(string filePath, bool isCompressed, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }
        
        // Determine if file is compressed by extension or flag
        var hasGzExtension = filePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
        var shouldDecompress = isCompressed || hasGzExtension;
        
        byte[] content;
        string originalFileName;
        
        if (shouldDecompress && hasGzExtension)
        {
            // Decompress the file
            await using var compressedStream = File.OpenRead(filePath);
            await using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            await using var memoryStream = new MemoryStream();
            
            await gzipStream.CopyToAsync(memoryStream, cancellationToken);
            content = memoryStream.ToArray();
            
            // Extract original filename (remove .gz extension)
            originalFileName = Path.GetFileNameWithoutExtension(filePath);
            
            _logger.LogDebug("File decompressed: {FilePath}, Decompressed size: {Size} bytes", filePath, content.Length);
        }
        else
        {
            // Read file normally (not compressed)
            content = await File.ReadAllBytesAsync(filePath, cancellationToken);
            originalFileName = Path.GetFileName(filePath);
        }
        
        return new FileData(content, originalFileName, shouldDecompress);
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
            // Check if file is compressed (has .gz extension or needs to check actual file)
            var actualImagePath = imagePath;
            var isCompressed = false;
            
            if (File.Exists(imagePath + ".gz"))
            {
                actualImagePath = imagePath + ".gz";
                isCompressed = true;
            }
            else if (!File.Exists(actualImagePath))
            {
                throw new FileNotFoundException($"Image file not found: {imagePath}");
            }
            
            // Compute hash on actual file (compressed or not) for cache key
            var fileHash = await ComputeHashAsync(actualImagePath, cancellationToken);
            var hashHex = Convert.ToHexString(fileHash);
            var thumbnailPath = Path.Combine(_thumbnailDirectory, $"{hashHex}.jpg");
            
            if (File.Exists(thumbnailPath))
            {
                return thumbnailPath;
            }
            
            // For thumbnails, we need to read the decompressed content
            var fileData = await ReadFileAsync(actualImagePath, isCompressed, cancellationToken);
            
            // Load image from decompressed content
            await using var imageStream = new MemoryStream(fileData.Content);
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

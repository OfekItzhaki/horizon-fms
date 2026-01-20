using MediatR;
using Microsoft.Extensions.Logging;
using FileManagementSystem.Application.Commands;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Domain.Entities;
using FileManagementSystem.Domain.Exceptions;

namespace FileManagementSystem.Application.Handlers;

public class UploadFileCommandHandler : IRequestHandler<UploadFileCommand, UploadFileResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storageService;
    private readonly IMetadataService _metadataService;
    private readonly ILogger<UploadFileCommandHandler> _logger;
    
    public UploadFileCommandHandler(
        IUnitOfWork unitOfWork,
        IStorageService storageService,
        IMetadataService metadataService,
        ILogger<UploadFileCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _metadataService = metadataService;
        _logger = logger;
    }
    
    public async Task<UploadFileResult> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Uploading file: {SourcePath}", request.SourcePath);
        
        // Validate and normalize source path
        var normalizedSourcePath = Path.GetFullPath(request.SourcePath);
        
        // Basic path validation
        if (request.SourcePath.Contains("..") || request.SourcePath.Contains("~"))
        {
            _logger.LogWarning("Path traversal attempt detected: {Path}", request.SourcePath);
            throw new UnauthorizedAccessException($"Invalid file path: {request.SourcePath}");
        }
        
        if (!File.Exists(normalizedSourcePath))
        {
            throw new FileNotFoundException($"Source file not found: {normalizedSourcePath}");
        }
        
        // Check if file already exists in database
        var existingFile = await _unitOfWork.Files.FindAsync(
            f => f.Path == normalizedSourcePath, cancellationToken);
        
        if (existingFile.Any())
        {
            _logger.LogWarning("File already exists in database: {FilePath}", normalizedSourcePath);
            return new UploadFileResult(existingFile.First().Id, true, normalizedSourcePath);
        }
        
        // Compute hash to check for duplicates
        var hash = await _storageService.ComputeHashAsync(normalizedSourcePath, cancellationToken);
        var hashHex = Convert.ToHexString(hash);
        var duplicate = await _unitOfWork.Files.GetByHashAsync(hash, cancellationToken);
        
        if (duplicate != null)
        {
            _logger.LogWarning("Duplicate file detected by hash: {FilePath} (existing: {ExistingPath})", 
                normalizedSourcePath, duplicate.Path);
            throw new FileDuplicateException(normalizedSourcePath, hash);
        }
        
        // Determine destination path - always store in managed storage location
        string destinationPath;
        var storageBasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FileManagementSystem",
            "Storage"
        );
        
        Folder? targetFolder = null;
        
        // If a folder ID is provided, use that folder
        if (request.DestinationFolderId.HasValue)
        {
            targetFolder = await _unitOfWork.Folders.GetByIdAsync(request.DestinationFolderId.Value, cancellationToken);
            if (targetFolder == null)
            {
                _logger.LogWarning("Destination folder not found: {FolderId}, using default folder", request.DestinationFolderId.Value);
            }
        }
        
        // If no folder specified or folder not found, use/create "Default" folder
        if (targetFolder == null)
        {
            var defaultFolderPath = Path.Combine(storageBasePath, "Default");
            try
            {
                targetFolder = await _unitOfWork.Folders.GetOrCreateByPathAsync(defaultFolderPath, cancellationToken);
                _logger.LogInformation("Using default folder for upload: {FolderPath}", defaultFolderPath);
            }
            catch (InvalidOperationException)
            {
                // If default folder creation fails, create it manually
                targetFolder = new Folder
                {
                    Name = "Default",
                    Path = defaultFolderPath,
                    ParentFolderId = null,
                    CreatedDate = DateTime.UtcNow
                };
                await _unitOfWork.Folders.AddAsync(targetFolder, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Created default folder: {FolderPath}", defaultFolderPath);
            }
        }
        
        // Build destination path using the target folder
        var fileName = Path.GetFileName(normalizedSourcePath);
        destinationPath = Path.Combine(targetFolder.Path, fileName);
        
        // Ensure the directory exists
        Directory.CreateDirectory(targetFolder.Path);
        
        // Always copy file to managed storage location (now compresses automatically)
        var compressedPath = await _storageService.SaveFileAsync(normalizedSourcePath, destinationPath, cancellationToken);
        
        // Get original filename from source file (used for display and MIME type)
        var originalFileName = Path.GetFileName(normalizedSourcePath);
        var displayPath = destinationPath; // Store storage path (without .gz) for file location
        
        // Use the target folder we already determined
        var folder = targetFolder;
        
        // Extract metadata for photos (need to decompress temporarily or read from original)
        // For now, we'll extract metadata from the original source file before compression
        var isPhoto = await _metadataService.IsPhotoFileAsync(normalizedSourcePath, cancellationToken);
        PhotoMetadata? photoMetadata = null;
        
        if (isPhoto)
        {
            photoMetadata = await _metadataService.ExtractPhotoMetadataAsync(normalizedSourcePath, cancellationToken);
        }
        
        // Create file item
        var compressedFileInfo = new FileInfo(compressedPath);
        var fileItem = new FileItem
        {
            Path = displayPath, // Store storage path for file location (without .gz)
            FileName = originalFileName, // Store original filename for display
            Hash = hash,
            HashHex = hashHex,
            Size = compressedFileInfo.Length, // Store compressed size (actual disk usage)
            IsCompressed = true, // Mark as compressed
            MimeType = GetMimeType(originalFileName), // Use original filename for MIME type
            IsPhoto = isPhoto,
            FolderId = folder?.Id,
            CreatedDate = DateTime.UtcNow,
            PhotoDateTaken = photoMetadata?.DateTaken,
            CameraMake = photoMetadata?.CameraMake,
            CameraModel = photoMetadata?.CameraModel,
            Latitude = photoMetadata?.Longitude,
            Longitude = photoMetadata?.Longitude
        };
        
        await _unitOfWork.Files.AddAsync(fileItem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("File uploaded successfully: {FilePath} (ID: {FileId})", 
            destinationPath, fileItem.Id);
        
        return new UploadFileResult(fileItem.Id, false, destinationPath);
    }
    
    private static string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
    }
}

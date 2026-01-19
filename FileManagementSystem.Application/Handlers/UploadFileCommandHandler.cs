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
        
        // Determine destination path
        string destinationPath;
        if (!string.IsNullOrEmpty(request.DestinationFolder))
        {
            var destFolder = Path.GetFullPath(request.DestinationFolder);
            Directory.CreateDirectory(destFolder);
            
            var fileName = Path.GetFileName(normalizedSourcePath);
            destinationPath = Path.Combine(destFolder, fileName);
        }
        else if (request.OrganizeByDate)
        {
            // Organize by date from file metadata or file system
            var fileInfo = new FileInfo(normalizedSourcePath);
            var dateFolder = fileInfo.CreationTime.ToString("yyyy-MM");
            var baseFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                "FileManagementSystem", "Organized", dateFolder);
            Directory.CreateDirectory(baseFolder);
            
            destinationPath = Path.Combine(baseFolder, Path.GetFileName(normalizedSourcePath));
        }
        else
        {
            // Keep original location
            destinationPath = normalizedSourcePath;
        }
        
        // Copy file if destination is different
        if (destinationPath != normalizedSourcePath)
        {
            destinationPath = await _storageService.SaveFileAsync(normalizedSourcePath, destinationPath, cancellationToken);
        }
        
        // Get or create folder
        var folderPath = Path.GetDirectoryName(destinationPath);
        var folder = folderPath != null 
            ? await _unitOfWork.Folders.GetOrCreateByPathAsync(folderPath, cancellationToken)
            : null;
        
        // Extract metadata for photos
        var isPhoto = await _metadataService.IsPhotoFileAsync(destinationPath, cancellationToken);
        PhotoMetadata? photoMetadata = null;
        
        if (isPhoto)
        {
            photoMetadata = await _metadataService.ExtractPhotoMetadataAsync(destinationPath, cancellationToken);
        }
        
        // Create file item
        var destFileInfo = new FileInfo(destinationPath);
        var fileItem = new FileItem
        {
            Path = destinationPath,
            Hash = hash,
            HashHex = hashHex,
            Size = destFileInfo.Length,
            MimeType = GetMimeType(destinationPath),
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

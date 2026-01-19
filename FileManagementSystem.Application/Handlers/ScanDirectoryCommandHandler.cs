using MediatR;
using Microsoft.Extensions.Logging;
using FileManagementSystem.Application.Commands;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Application.DTOs;
using FileManagementSystem.Domain.Entities;
using FileManagementSystem.Domain.Exceptions;

namespace FileManagementSystem.Application.Handlers;

public class ScanDirectoryCommandHandler : IRequestHandler<ScanDirectoryCommand, ScanDirectoryResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMetadataService _metadataService;
    private readonly IStorageService _storageService;
    private readonly ILogger<ScanDirectoryCommandHandler> _logger;
    
    public ScanDirectoryCommandHandler(
        IUnitOfWork unitOfWork,
        IMetadataService metadataService,
        IStorageService storageService,
        ILogger<ScanDirectoryCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _metadataService = metadataService;
        _storageService = storageService;
        _logger = logger;
    }
    
    public async Task<ScanDirectoryResult> Handle(ScanDirectoryCommand request, CancellationToken cancellationToken)
    {
        // Validate and normalize path
        var directoryPath = Path.GetFullPath(request.DirectoryPath);
        
        // Basic path validation - check for traversal attempts
        if (request.DirectoryPath.Contains("..") || request.DirectoryPath.Contains("~"))
        {
            _logger.LogWarning("Path traversal attempt detected: {Path}", request.DirectoryPath);
            throw new UnauthorizedAccessException($"Invalid directory path: {request.DirectoryPath}");
        }
        
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
        }
        
        var filesProcessed = 0;
        var filesSkipped = 0;
        var foldersCreated = 0;
        
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            
            var allFiles = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            var totalFiles = allFiles.Length;
            
            request.Progress?.Report(new ProgressReportDto
            {
                ProcessedItems = 0,
                TotalItems = totalFiles,
                CurrentItem = "Starting scan...",
                IsCompleted = false
            });
            
            foreach (var filePath in allFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                
                try
                {
                    var normalizedPath = Path.GetFullPath(filePath);
                    
                    // Check if file already exists
                    var existingFile = await _unitOfWork.Files.FindAsync(
                        f => f.Path == normalizedPath, cancellationToken);
                    
                    if (existingFile.Any())
                    {
                        filesSkipped++;
                        continue;
                    }
                    
                    // Compute hash
                    var hash = await _storageService.ComputeHashAsync(normalizedPath, cancellationToken);
                    var hashHex = Convert.ToHexString(hash);
                    
                    // Check for duplicate by hash
                    var duplicate = await _unitOfWork.Files.GetByHashAsync(hash, cancellationToken);
                    if (duplicate != null)
                    {
                        _logger.LogWarning("Duplicate file detected: {FilePath} (hash matches: {ExistingPath})", 
                            normalizedPath, duplicate.Path);
                        filesSkipped++;
                        continue;
                    }
                    
                    // Get or create folder
                    var folderPath = Path.GetDirectoryName(normalizedPath);
                    var folder = folderPath != null 
                        ? await _unitOfWork.Folders.GetOrCreateByPathAsync(folderPath, cancellationToken)
                        : null;
                    
                    if (folder != null && folder.Id == Guid.Empty)
                    {
                        foldersCreated++;
                    }
                    
                    // Extract metadata for photos
                    var isPhoto = await _metadataService.IsPhotoFileAsync(normalizedPath, cancellationToken);
                    PhotoMetadata? photoMetadata = null;
                    
                    if (isPhoto)
                    {
                        photoMetadata = await _metadataService.ExtractPhotoMetadataAsync(normalizedPath, cancellationToken);
                    }
                    
                    // Create file item
                    var fileInfo = new FileInfo(normalizedPath);
                    var fileItem = new FileItem
                    {
                        Path = normalizedPath,
                        Hash = hash,
                        HashHex = hashHex,
                        Size = fileInfo.Length,
                        MimeType = GetMimeType(normalizedPath),
                        IsPhoto = isPhoto,
                        FolderId = folder?.Id,
                        CreatedDate = DateTime.UtcNow,
                        PhotoDateTaken = photoMetadata?.DateTaken,
                        CameraMake = photoMetadata?.CameraMake,
                        CameraModel = photoMetadata?.CameraModel,
                        Latitude = photoMetadata?.Latitude,
                        Longitude = photoMetadata?.Longitude
                    };
                    
                    await _unitOfWork.Files.AddAsync(fileItem, cancellationToken);
                    filesProcessed++;
                    
                    // Batch save every 100 files for performance
                    if (filesProcessed % 100 == 0)
                    {
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                    
                    request.Progress?.Report(new ProgressReportDto
                    {
                        ProcessedItems = filesProcessed + filesSkipped,
                        TotalItems = totalFiles,
                        CurrentItem = Path.GetFileName(normalizedPath),
                        IsCompleted = false
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing file: {FilePath}", filePath);
                    filesSkipped++;
                }
            }
            
            // Final save for remaining files
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            
            _logger.LogInformation("Scan completed: {Processed} processed, {Skipped} skipped, {Folders} folders created",
                filesProcessed, filesSkipped, foldersCreated);
            
            request.Progress?.Report(new ProgressReportDto
            {
                ProcessedItems = totalFiles,
                TotalItems = totalFiles,
                CurrentItem = "Complete",
                IsCompleted = true
            });
            
            return new ScanDirectoryResult(filesProcessed, filesSkipped, foldersCreated);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
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

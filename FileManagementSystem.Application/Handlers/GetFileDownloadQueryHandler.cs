using MediatR;
using Microsoft.Extensions.Logging;
using FileManagementSystem.Application.Queries;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Application.DTOs;
using System.IO;

namespace FileManagementSystem.Application.Handlers;

public class GetFileDownloadQueryHandler : IRequestHandler<GetFileDownloadQuery, FileDownloadResultDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storageService;
    private readonly IFilePathResolver _filePathResolver;
    private readonly ILogger<GetFileDownloadQueryHandler> _logger;

    public GetFileDownloadQueryHandler(
        IUnitOfWork unitOfWork,
        IStorageService storageService,
        IFilePathResolver filePathResolver,
        ILogger<GetFileDownloadQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _filePathResolver = filePathResolver;
        _logger = logger;
    }

    public async Task<FileDownloadResultDto?> Handle(GetFileDownloadQuery request, CancellationToken cancellationToken)
    {
        var file = await _unitOfWork.Files.GetByIdAsync(request.FileId, cancellationToken);
        if (file == null)
        {
            _logger.LogWarning("Download attempt for non-existent file ID: {FileId}", request.FileId);
            return null;
        }

        var storedPath = file.Path;
        _logger.LogInformation("Processing download: FileId={FileId}, StoredPath={StoredPath}, IsCompressed={IsCompressed}", 
            request.FileId, storedPath, file.IsCompressed);
        
        var actualFilePath = _filePathResolver.ResolveFilePath(storedPath, file.IsCompressed);

        if (actualFilePath == null)
        {
            _logger.LogError("File not found on server for download: ID={FileId}, Path={Path}", request.FileId, storedPath);
            return null;
        }

        // Read file (automatically decompresses if needed)
        var isActuallyCompressed = actualFilePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
        var fileData = await _storageService.ReadFileAsync(actualFilePath, isActuallyCompressed, cancellationToken);
        
        // Use the original filename from database if available
        var downloadFileName = !string.IsNullOrEmpty(file.FileName) 
            ? file.FileName 
            : fileData.OriginalFileName;
        
        return new FileDownloadResultDto(
            Content: fileData.Content,
            MimeType: file.MimeType,
            FileName: downloadFileName,
            WasCompressed: file.IsCompressed
        );
    }
}

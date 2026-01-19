using MediatR;
using Microsoft.Extensions.Logging;
using FileManagementSystem.Application.Commands;
using FileManagementSystem.Application.Interfaces;

namespace FileManagementSystem.Application.Handlers;

public class RenameFileCommandHandler : IRequestHandler<RenameFileCommand, RenameFileResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RenameFileCommandHandler> _logger;
    
    public RenameFileCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<RenameFileCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    
    public async Task<RenameFileResult> Handle(RenameFileCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Renaming file {FileId} to {NewName}", request.FileId, request.NewName);
        
        var file = await _unitOfWork.Files.GetByIdAsync(request.FileId, cancellationToken);
        if (file == null)
        {
            _logger.LogWarning("File not found for rename: {FileId}", request.FileId);
            return new RenameFileResult(false, string.Empty);
        }
        
        // Validate new name
        if (string.IsNullOrWhiteSpace(request.NewName) || 
            request.NewName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException($"Invalid file name: {request.NewName}");
        }
        
        var directory = Path.GetDirectoryName(file.Path);
        var extension = Path.GetExtension(file.Path);
        var newPath = Path.Combine(directory ?? "", $"{request.NewName}{extension}");
        
        // Normalize and validate new path
        newPath = Path.GetFullPath(newPath);
        
        if (newPath.Contains("..") || newPath.Contains("~"))
        {
            throw new UnauthorizedAccessException($"Invalid path: {newPath}");
        }
        
        if (File.Exists(newPath))
        {
            throw new InvalidOperationException($"File already exists: {newPath}");
        }
        
        // Rename physical file using async operations
        if (File.Exists(file.Path))
        {
            await Task.Run(() => File.Move(file.Path, newPath), cancellationToken);
        }
        
        // Update database
        file.Path = newPath;
        await _unitOfWork.Files.UpdateAsync(file, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("File renamed successfully: {OldPath} -> {NewPath}", file.Path, newPath);
        
        return new RenameFileResult(true, newPath);
    }
}

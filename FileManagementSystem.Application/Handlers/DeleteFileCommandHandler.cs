using MediatR;
using Microsoft.Extensions.Logging;
using FileManagementSystem.Application.Commands;
using FileManagementSystem.Application.Interfaces;

namespace FileManagementSystem.Application.Handlers;

public class DeleteFileCommandHandler : IRequestHandler<DeleteFileCommand, DeleteFileResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storageService;
    private readonly ILogger<DeleteFileCommandHandler> _logger;
    
    public DeleteFileCommandHandler(
        IUnitOfWork unitOfWork,
        IStorageService storageService,
        ILogger<DeleteFileCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _storageService = storageService;
        _logger = logger;
    }
    
    public async Task<DeleteFileResult> Handle(DeleteFileCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting file {FileId}, MoveToRecycleBin: {MoveToRecycleBin}", 
            request.FileId, request.MoveToRecycleBin);
        
        var file = await _unitOfWork.Files.GetByIdAsync(request.FileId, cancellationToken);
        if (file == null)
        {
            _logger.LogWarning("File not found for deletion: {FileId}", request.FileId);
            return new DeleteFileResult(false, null);
        }
        
        var filePath = file.Path;
        
        // Delete from database
        await _unitOfWork.Files.DeleteAsync(file, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Delete physical file
        if (File.Exists(filePath))
        {
            var deleted = await _storageService.DeleteFileAsync(filePath, request.MoveToRecycleBin, cancellationToken);
            if (deleted)
            {
                _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
            }
            else
            {
                _logger.LogWarning("Failed to delete physical file: {FilePath}", filePath);
            }
        }
        else
        {
            _logger.LogWarning("Physical file not found: {FilePath}", filePath);
        }
        
        return new DeleteFileResult(true, filePath);
    }
}

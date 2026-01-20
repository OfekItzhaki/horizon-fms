using MediatR;
using Microsoft.Extensions.Logging;
using FileManagementSystem.Application.Commands;
using FileManagementSystem.Application.Interfaces;

namespace FileManagementSystem.Application.Handlers;

public class DeleteFolderCommandHandler : IRequestHandler<DeleteFolderCommand, DeleteFolderResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteFolderCommandHandler> _logger;
    
    public DeleteFolderCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteFolderCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    
    public async Task<DeleteFolderResult> Handle(DeleteFolderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting folder {FolderId}, DeleteFiles: {DeleteFiles}", 
            request.FolderId, request.DeleteFiles);
        
        var folder = await _unitOfWork.Folders.GetByIdAsync(request.FolderId, cancellationToken);
        if (folder == null)
        {
            _logger.LogWarning("Folder not found for deletion: {FolderId}", request.FolderId);
            return new DeleteFolderResult(false, "Folder not found");
        }
        
        // Check if folder has subfolders
        var subFolders = await _unitOfWork.Folders.GetByParentIdAsync(request.FolderId, cancellationToken);
        if (subFolders.Count > 0)
        {
            return new DeleteFolderResult(false, "Cannot delete folder: it contains subfolders. Please delete subfolders first.");
        }
        
        // Check if folder has files
        var filesInFolder = await _unitOfWork.Files.FindAsync(
            f => f.FolderId == request.FolderId, 
            cancellationToken);
        
        if (filesInFolder.Any())
        {
            if (!request.DeleteFiles)
            {
                return new DeleteFolderResult(false, $"Cannot delete folder: it contains {filesInFolder.Count()} file(s). Set deleteFiles=true to delete the folder and its files.");
            }
            
            // Delete all files in the folder
            _logger.LogInformation("Deleting {Count} files in folder", filesInFolder.Count());
            foreach (var file in filesInFolder)
            {
                await _unitOfWork.Files.DeleteAsync(file, cancellationToken);
            }
        }
        
        // Delete the folder
        await _unitOfWork.Folders.DeleteAsync(folder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Folder deleted successfully: {FolderId}, Path: {Path}", folder.Id, folder.Path);
        return new DeleteFolderResult(true);
    }
}

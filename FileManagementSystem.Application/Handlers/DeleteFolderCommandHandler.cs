using System.IO;
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
        
        // Prevent deletion of "Default" folder
        // Check by name and by path - more robust check
        var storageBasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FileManagementSystem",
            "Storage"
        );
        var expectedDefaultPath = Path.Combine(storageBasePath, "Default");
        
        // Normalize paths for comparison
        var normalizedFolderPath = Path.GetFullPath(folder.Path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var normalizedDefaultPath = Path.GetFullPath(expectedDefaultPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        
        var isDefaultFolder = folder.Name.Equals("Default", StringComparison.OrdinalIgnoreCase) ||
            normalizedFolderPath.Equals(normalizedDefaultPath, StringComparison.OrdinalIgnoreCase) ||
            normalizedFolderPath.EndsWith(Path.DirectorySeparatorChar + "Default", StringComparison.OrdinalIgnoreCase) ||
            normalizedFolderPath.EndsWith(Path.AltDirectorySeparatorChar + "Default", StringComparison.OrdinalIgnoreCase) ||
            folder.Path.Equals("Default", StringComparison.OrdinalIgnoreCase);
        
        if (isDefaultFolder)
        {
            _logger.LogWarning("Attempt to delete Default folder blocked: {FolderId}, Name: {Name}, Path: {Path}", 
                folder.Id, folder.Name, folder.Path);
            return new DeleteFolderResult(false, "Cannot delete the Default folder. This folder is required by the system.");
        }
        
        // Get subfolders
        var subFolders = await _unitOfWork.Folders.GetByParentIdAsync(request.FolderId, cancellationToken);
        
        // If folder has subfolders and deleteFiles is false, prevent deletion
        if (subFolders.Count > 0 && !request.DeleteFiles)
        {
            return new DeleteFolderResult(false, "Cannot delete folder: it contains subfolders. Set deleteFiles=true to delete the folder, its subfolders, and files.");
        }
        
        // If deleteFiles is true, recursively delete subfolders and their files
        if (subFolders.Count > 0 && request.DeleteFiles)
        {
            _logger.LogInformation("Deleting {Count} subfolders recursively", subFolders.Count);
            foreach (var subFolder in subFolders)
            {
                // Recursively delete subfolder with its files
                var subFolderCommand = new DeleteFolderCommand(subFolder.Id, DeleteFiles: true);
                var subFolderResult = await Handle(subFolderCommand, cancellationToken);
                if (!subFolderResult.Success)
                {
                    _logger.LogWarning("Failed to delete subfolder {SubFolderId}: {Error}", subFolder.Id, subFolderResult.ErrorMessage);
                    // Continue with other subfolders
                }
            }
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

using System.IO;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace FileManagementSystem.Application.Services;

public class UploadDestinationResolver
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UploadDestinationResolver> _logger;

    public UploadDestinationResolver(IUnitOfWork unitOfWork, ILogger<UploadDestinationResolver> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Folder> ResolveDestinationFolderAsync(Guid? destinationFolderId, CancellationToken cancellationToken)
    {
        Folder? targetFolder = null;

        // If a folder ID is provided, use that folder
        if (destinationFolderId.HasValue)
        {
            targetFolder = await _unitOfWork.Folders.GetByIdAsync(destinationFolderId.Value, cancellationToken);
            if (targetFolder == null)
            {
                _logger.LogWarning("Destination folder not found: {FolderId}, using default folder", destinationFolderId.Value);
            }
        }

        // If no folder specified or folder not found, use/create "Default" folder
        if (targetFolder == null)
        {
            targetFolder = await GetOrCreateDefaultFolderAsync(cancellationToken);
        }

        return targetFolder;
    }

    private async Task<Folder> GetOrCreateDefaultFolderAsync(CancellationToken cancellationToken)
    {
        var storageBasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FileManagementSystem",
            "Storage"
        );
        var defaultFolderPath = Path.Combine(storageBasePath, "Default");

        // Ensure the physical directory exists
        Directory.CreateDirectory(defaultFolderPath);

        // Try to get existing folder by path
        var existingFolder = await _unitOfWork.Folders.GetByPathAsync(defaultFolderPath, cancellationToken);
        if (existingFolder != null)
        {
            _logger.LogInformation("Using existing default folder: {FolderPath}", defaultFolderPath);
            return existingFolder;
        }

        // Try to get by name "Default"
        var folders = await _unitOfWork.Folders.FindAsync(
            f => f.Name == "Default", 
            cancellationToken);
        var defaultByName = folders.FirstOrDefault();
        
        if (defaultByName != null)
        {
            // Update path if it's different
            if (defaultByName.Path != defaultFolderPath)
            {
                defaultByName.Path = defaultFolderPath;
                await _unitOfWork.Folders.UpdateAsync(defaultByName, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Updated Default folder path to: {FolderPath}", defaultFolderPath);
            }
            return defaultByName;
        }

        // Create new Default folder
        try
        {
            var folder = await _unitOfWork.Folders.GetOrCreateByPathAsync(defaultFolderPath, cancellationToken);
            _logger.LogInformation("Created/retrieved default folder: {FolderPath}", defaultFolderPath);
            return folder;
        }
        catch (InvalidOperationException ex)
        {
            // If GetOrCreateByPathAsync fails (e.g., path validation), create it directly
            _logger.LogWarning(ex, "GetOrCreateByPathAsync failed for Default folder, creating directly");
            var folder = new Folder
            {
                Name = "Default",
                Path = defaultFolderPath,
                ParentFolderId = null,
                CreatedDate = DateTime.UtcNow
            };
            await _unitOfWork.Folders.AddAsync(folder, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Created default folder directly: {FolderPath}", defaultFolderPath);
            return folder;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating Default folder");
            throw;
        }
    }
}

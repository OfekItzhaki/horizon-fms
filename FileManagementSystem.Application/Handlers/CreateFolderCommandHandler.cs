using MediatR;
using Microsoft.Extensions.Logging;
using FileManagementSystem.Application.Commands;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Application.Mappings;
using FileManagementSystem.Domain.Entities;

namespace FileManagementSystem.Application.Handlers;

public class CreateFolderCommandHandler : IRequestHandler<CreateFolderCommand, CreateFolderResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateFolderCommandHandler> _logger;
    
    public CreateFolderCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreateFolderCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    
    public async Task<CreateFolderResult> Handle(CreateFolderCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating folder: {Name}, ParentFolderId: {ParentFolderId}", 
            request.Name, request.ParentFolderId);
        
        // Validate folder name
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Folder name cannot be empty", nameof(request.Name));
        }
        
        // Check if folder with same name already exists under the parent
        var existingFolders = await _unitOfWork.Folders.GetByParentIdAsync(request.ParentFolderId, cancellationToken);
        if (existingFolders.Any(f => f.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"A folder with the name '{request.Name}' already exists in this location");
        }
        
        // Build the folder path
        string folderPath;
        Folder? parentFolder = null;
        
        if (request.ParentFolderId.HasValue)
        {
            parentFolder = await _unitOfWork.Folders.GetByIdAsync(request.ParentFolderId.Value, cancellationToken);
            if (parentFolder == null)
            {
                throw new ArgumentException($"Parent folder with ID {request.ParentFolderId} not found", nameof(request.ParentFolderId));
            }
            
            // Build path: parent path + folder name
            var parentPath = parentFolder.Path.TrimEnd('/', '\\');
            folderPath = parentPath + Path.DirectorySeparatorChar + request.Name;
        }
        else
        {
            // Root level folder - use just the name
            folderPath = request.Name;
        }
        
        // Create the folder
        var folder = new Folder
        {
            Name = request.Name,
            Path = folderPath,
            ParentFolderId = request.ParentFolderId,
            CreatedDate = DateTime.UtcNow
        };
        
        await _unitOfWork.Folders.AddAsync(folder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Folder created successfully: {FolderId}, Path: {Path}", folder.Id, folder.Path);
        
        var folderDto = folder.ToDto(0, 0); // New folder has no files or subfolders yet
        return new CreateFolderResult(folder.Id, folderDto);
    }
}

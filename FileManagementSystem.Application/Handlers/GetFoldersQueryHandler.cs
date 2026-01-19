using MediatR;
using Microsoft.Extensions.Logging;
using FileManagementSystem.Application.Queries;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Application.Mappings;

namespace FileManagementSystem.Application.Handlers;

public class GetFoldersQueryHandler : IRequestHandler<GetFoldersQuery, GetFoldersResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetFoldersQueryHandler> _logger;
    
    public GetFoldersQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetFoldersQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    
    public async Task<GetFoldersResult> Handle(GetFoldersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting folders for parent: {ParentFolderId}", request.ParentFolderId);
        
        var folders = await _unitOfWork.Folders.GetByParentIdAsync(request.ParentFolderId, cancellationToken);
        
        var folderDtos = folders.Select(f =>
        {
            var fileCount = f.Files?.Count ?? 0;
            var subFolderCount = f.SubFolders?.Count ?? 0;
            return f.ToDto(fileCount, subFolderCount);
        }).ToList();
        
        _logger.LogDebug("Retrieved {Count} folders", folderDtos.Count);
        
        return new GetFoldersResult(folderDtos);
    }
}

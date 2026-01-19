using MediatR;
using Microsoft.Extensions.Logging;
using FileManagementSystem.Application.Queries;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Application.Mappings;
using FileManagementSystem.Application.DTOs;

namespace FileManagementSystem.Application.Handlers;

public class GetFileQueryHandler : IRequestHandler<GetFileQuery, FileItemDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetFileQueryHandler> _logger;
    
    public GetFileQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetFileQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    
    public async Task<FileItemDto?> Handle(GetFileQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Getting file: {FileId}", request.FileId);
        
        var file = await _unitOfWork.Files.GetByIdAsync(request.FileId, cancellationToken);
        
        if (file == null)
        {
            _logger.LogWarning("File not found: {FileId}", request.FileId);
            return null;
        }
        
        return file.ToDto();
    }
}

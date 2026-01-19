using MediatR;
using Microsoft.Extensions.Logging;
using FileManagementSystem.Application.Commands;
using FileManagementSystem.Application.Interfaces;

namespace FileManagementSystem.Application.Handlers;

public class AddTagsCommandHandler : IRequestHandler<AddTagsCommand, AddTagsResult>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddTagsCommandHandler> _logger;
    
    public AddTagsCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<AddTagsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
    
    public async Task<AddTagsResult> Handle(AddTagsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding tags to file {FileId}: {Tags}", 
            request.FileId, string.Join(", ", request.Tags));
        
        var file = await _unitOfWork.Files.GetByIdAsync(request.FileId, cancellationToken);
        if (file == null)
        {
            _logger.LogWarning("File not found for adding tags: {FileId}", request.FileId);
            return new AddTagsResult(false);
        }
        
        var tagsAdded = 0;
        foreach (var tag in request.Tags)
        {
            if (!file.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
            {
                file.Tags.Add(tag);
                tagsAdded++;
            }
        }
        
        if (tagsAdded > 0)
        {
            await _unitOfWork.Files.UpdateAsync(file, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Added {Count} tags to file {FileId}", tagsAdded, request.FileId);
        }
        else
        {
            _logger.LogInformation("No new tags to add for file {FileId}", request.FileId);
        }
        
        return new AddTagsResult(true);
    }
}

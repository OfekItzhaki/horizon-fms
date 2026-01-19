using MediatR;
using FileManagementSystem.Application.Queries;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Application.Mappings;

namespace FileManagementSystem.Application.Handlers;

public class SearchFilesQueryHandler : IRequestHandler<SearchFilesQuery, SearchFilesResult>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public SearchFilesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<SearchFilesResult> Handle(SearchFilesQuery request, CancellationToken cancellationToken)
    {
        var files = await _unitOfWork.Files.SearchAsync(
            request.SearchTerm,
            request.Tags,
            request.IsPhoto,
            request.FolderId,
            request.Skip,
            request.Take,
            cancellationToken);
        
        var totalCount = await _unitOfWork.Files.CountAsync(
            request.SearchTerm,
            request.Tags,
            request.IsPhoto,
            request.FolderId,
            cancellationToken);
        
        var fileDtos = files.Select(f => f.ToDto()).ToList();
        
        return new SearchFilesResult(fileDtos, totalCount);
    }
}

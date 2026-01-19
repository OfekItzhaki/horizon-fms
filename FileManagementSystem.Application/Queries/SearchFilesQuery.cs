using MediatR;
using FileManagementSystem.Application.DTOs;

namespace FileManagementSystem.Application.Queries;

public record SearchFilesQuery(
    string? SearchTerm = null,
    List<string>? Tags = null,
    bool? IsPhoto = null,
    Guid? FolderId = null,
    int Skip = 0,
    int Take = 100) 
    : IRequest<SearchFilesResult>;

public record SearchFilesResult(IReadOnlyList<FileItemDto> Files, int TotalCount);

using MediatR;
using FileManagementSystem.Application.DTOs;

namespace FileManagementSystem.Application.Queries;

public record GetFoldersQuery(Guid? ParentFolderId = null) : IRequest<GetFoldersResult>;

public record GetFoldersResult(IReadOnlyList<FolderDto> Folders);

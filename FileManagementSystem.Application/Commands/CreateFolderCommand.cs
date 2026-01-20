using MediatR;
using FileManagementSystem.Application.DTOs;

namespace FileManagementSystem.Application.Commands;

public record CreateFolderCommand(string Name, Guid? ParentFolderId = null) : IRequest<CreateFolderResult>;

public record CreateFolderResult(Guid FolderId, FolderDto Folder);

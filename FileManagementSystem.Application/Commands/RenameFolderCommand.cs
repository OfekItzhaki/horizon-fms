using MediatR;
using FileManagementSystem.Application.DTOs;

namespace FileManagementSystem.Application.Commands;

public record RenameFolderCommand(Guid FolderId, string NewName) : IRequest<RenameFolderResult>;

public record RenameFolderResult(bool Success, FolderDto? Folder, string? ErrorMessage = null);

using MediatR;

namespace FileManagementSystem.Application.Commands;

public record DeleteFolderCommand(Guid FolderId, bool DeleteFiles = false) : IRequest<DeleteFolderResult>;

public record DeleteFolderResult(bool Success, string? ErrorMessage = null);

using MediatR;

namespace FileManagementSystem.Application.Commands;

public record DeleteFileCommand(Guid FileId, bool MoveToRecycleBin = true) : IRequest<DeleteFileResult>;

public record DeleteFileResult(bool Success, string? OriginalPath);

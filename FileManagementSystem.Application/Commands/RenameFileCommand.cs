using MediatR;

namespace FileManagementSystem.Application.Commands;

public record RenameFileCommand(Guid FileId, string NewName) : IRequest<RenameFileResult>;

public record RenameFileResult(bool Success, string NewPath);

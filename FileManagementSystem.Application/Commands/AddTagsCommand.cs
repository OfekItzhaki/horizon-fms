using MediatR;

namespace FileManagementSystem.Application.Commands;

public record AddTagsCommand(Guid FileId, List<string> Tags) : IRequest<AddTagsResult>;

public record AddTagsResult(bool Success);

using MediatR;

namespace FileManagementSystem.Application.Commands;

public record UploadFileCommand(string SourcePath, string OriginalFileName, Guid? DestinationFolderId = null, bool OrganizeByDate = false) 
    : IRequest<UploadFileResult>;

public record UploadFileResult(Guid FileId, bool IsDuplicate, string FilePath);

using MediatR;
using FileManagementSystem.Application.DTOs;

namespace FileManagementSystem.Application.Commands;

public record ScanDirectoryCommand(string DirectoryPath, IProgress<ProgressReportDto>? Progress = null) 
    : IRequest<ScanDirectoryResult>;

public record ScanDirectoryResult(int FilesProcessed, int FilesSkipped, int FoldersCreated);

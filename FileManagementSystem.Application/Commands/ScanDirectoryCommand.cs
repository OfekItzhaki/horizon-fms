using System;
using System.Reactive;
using MediatR;
using FileManagementSystem.Application.DTOs;
using FileManagementSystem.Application.Interfaces;

namespace FileManagementSystem.Application.Commands;

public record ScanDirectoryCommand(
    string DirectoryPath, 
    IProgress<ProgressReportDto>? Progress = null,
    IProgressObservable? ProgressObservable = null) 
    : IRequest<ScanDirectoryResult>;

public record ScanDirectoryResult(int FilesProcessed, int FilesSkipped, int FoldersCreated);

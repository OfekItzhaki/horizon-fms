using MediatR;
using FileManagementSystem.Application.DTOs;

namespace FileManagementSystem.Application.Queries;

public record GetFileDownloadQuery(Guid FileId) : IRequest<FileDownloadResultDto?>;

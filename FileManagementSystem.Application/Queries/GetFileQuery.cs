using MediatR;
using FileManagementSystem.Application.DTOs;

namespace FileManagementSystem.Application.Queries;

public record GetFileQuery(Guid FileId) : IRequest<FileItemDto?>;

using FileManagementSystem.Domain.Entities;

namespace FileManagementSystem.Application.Interfaces;

public interface IFileRepository : IRepository<FileItem>
{
    Task<FileItem?> GetByHashAsync(byte[] hash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FileItem>> SearchAsync(string? searchTerm, List<string>? tags, bool? isPhoto, Guid? folderId, int skip, int take, CancellationToken cancellationToken = default);
    Task<int> CountAsync(string? searchTerm, List<string>? tags, bool? isPhoto, Guid? folderId, CancellationToken cancellationToken = default);
}

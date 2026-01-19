using FileManagementSystem.Domain.Entities;

namespace FileManagementSystem.Application.Interfaces;

public interface IFolderRepository : IRepository<Folder>
{
    Task<Folder?> GetByPathAsync(string path, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Folder>> GetByParentIdAsync(Guid? parentFolderId, CancellationToken cancellationToken = default);
    Task<Folder> GetOrCreateByPathAsync(string path, CancellationToken cancellationToken = default);
}

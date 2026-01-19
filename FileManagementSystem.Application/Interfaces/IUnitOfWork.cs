namespace FileManagementSystem.Application.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    IFileRepository Files { get; }
    IFolderRepository Folders { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

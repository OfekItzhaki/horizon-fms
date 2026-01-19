using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Domain.Entities;
using FileManagementSystem.Infrastructure.Data;

namespace FileManagementSystem.Infrastructure.Repositories;

public class CachedFolderRepository : IFolderRepository
{
    private readonly FolderRepository _innerRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedFolderRepository> _logger;
    private const int CacheExpirationMinutes = 5;
    
    public CachedFolderRepository(
        AppDbContext context,
        IMemoryCache cache,
        ILogger<CachedFolderRepository> logger)
    {
        _innerRepository = new FolderRepository(context);
        _cache = cache;
        _logger = logger;
    }
    
    public async Task<Folder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"folder_{id}";
        if (_cache.TryGetValue<Folder>(cacheKey, out var cachedFolder))
        {
            _logger.LogDebug("Cache hit for folder: {FolderId}", id);
            return cachedFolder;
        }
        
        var folder = await _innerRepository.GetByIdAsync(id, cancellationToken);
        if (folder != null)
        {
            _cache.Set(cacheKey, folder, TimeSpan.FromMinutes(CacheExpirationMinutes));
        }
        
        return folder;
    }
    
    public async Task<IReadOnlyList<Folder>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _innerRepository.GetAllAsync(cancellationToken);
    }
    
    public async Task<IReadOnlyList<Folder>> FindAsync(System.Linq.Expressions.Expression<Func<Folder, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _innerRepository.FindAsync(predicate, cancellationToken);
    }
    
    public async Task<Folder> AddAsync(Folder entity, CancellationToken cancellationToken = default)
    {
        var result = await _innerRepository.AddAsync(entity, cancellationToken);
        _cache.Remove($"folder_{entity.Id}");
        return result;
    }
    
    public async Task UpdateAsync(Folder entity, CancellationToken cancellationToken = default)
    {
        await _innerRepository.UpdateAsync(entity, cancellationToken);
        _cache.Remove($"folder_{entity.Id}");
    }
    
    public async Task DeleteAsync(Folder entity, CancellationToken cancellationToken = default)
    {
        await _innerRepository.DeleteAsync(entity, cancellationToken);
        _cache.Remove($"folder_{entity.Id}");
    }
    
    public async Task<bool> ExistsAsync(System.Linq.Expressions.Expression<Func<Folder, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _innerRepository.ExistsAsync(predicate, cancellationToken);
    }
    
    public async Task<Folder?> GetByPathAsync(string path, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"folder_path_{path}";
        if (_cache.TryGetValue<Folder>(cacheKey, out var cachedFolder))
        {
            return cachedFolder;
        }
        
        var folder = await _innerRepository.GetByPathAsync(path, cancellationToken);
        if (folder != null)
        {
            _cache.Set(cacheKey, folder, TimeSpan.FromMinutes(CacheExpirationMinutes));
        }
        
        return folder;
    }
    
    public async Task<IReadOnlyList<Folder>> GetByParentIdAsync(Guid? parentFolderId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"folders_parent_{parentFolderId}";
        if (_cache.TryGetValue<IReadOnlyList<Folder>>(cacheKey, out var cachedFolders))
        {
            return cachedFolders!;
        }
        
        var folders = await _innerRepository.GetByParentIdAsync(parentFolderId, cancellationToken);
        _cache.Set(cacheKey, folders, TimeSpan.FromMinutes(CacheExpirationMinutes));
        
        return folders;
    }
    
    public async Task<Folder> GetOrCreateByPathAsync(string path, CancellationToken cancellationToken = default)
    {
        var folder = await _innerRepository.GetOrCreateByPathAsync(path, cancellationToken);
        _cache.Remove($"folder_path_{path}");
        _cache.Remove($"folder_{folder.Id}");
        return folder;
    }
}

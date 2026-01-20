using Microsoft.EntityFrameworkCore;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Domain.Entities;
using FileManagementSystem.Infrastructure.Data;

namespace FileManagementSystem.Infrastructure.Repositories;

public class FolderRepository : Repository<Folder>, IFolderRepository
{
    public FolderRepository(AppDbContext context) : base(context)
    {
    }
    
    public async Task<Folder?> GetByPathAsync(string path, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Path == path, cancellationToken);
    }
    
    public async Task<IReadOnlyList<Folder>> GetByParentIdAsync(Guid? parentFolderId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(f => f.ParentFolderId == parentFolderId)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<Folder> GetOrCreateByPathAsync(string path, CancellationToken cancellationToken = default)
    {
        var folder = await GetByPathAsync(path, cancellationToken);
        if (folder != null)
        {
            return folder;
        }
        
        var directoryInfo = new DirectoryInfo(path);
        
        // Prevent creating folders for drive letters (C:, D:, etc.)
        // A drive letter path like "C:" or "C:\" should not create a folder
        if (directoryInfo.Parent == null || 
            (directoryInfo.Parent.FullName == directoryInfo.Root.FullName && 
             directoryInfo.Name.Length <= 3 && directoryInfo.Name.EndsWith(":")))
        {
            // This is a root drive, don't create a folder for it
            // Return null or create a special root folder - for now, we'll skip drive letters
            throw new InvalidOperationException($"Cannot create folder for drive letter: {path}");
        }
        
        var parentPath = directoryInfo.Parent?.FullName;
        Folder? parentFolder = null;
        
        if (!string.IsNullOrEmpty(parentPath))
        {
            try
            {
                parentFolder = await GetOrCreateByPathAsync(parentPath, cancellationToken);
            }
            catch (InvalidOperationException)
            {
                // Parent is a drive letter, skip it and create this folder as root level
                parentFolder = null;
            }
        }
        
        folder = new Folder
        {
            Path = path,
            Name = directoryInfo.Name,
            ParentFolderId = parentFolder?.Id,
            CreatedDate = DateTime.UtcNow
        };
        
        await _dbSet.AddAsync(folder, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        
        // Reload to get the generated ID
        await _context.Entry(folder).ReloadAsync(cancellationToken);
        
        return folder;
    }
}

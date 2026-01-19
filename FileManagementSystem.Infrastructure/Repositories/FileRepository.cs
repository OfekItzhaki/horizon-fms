using Microsoft.EntityFrameworkCore;
using FileManagementSystem.Application.Interfaces;
using FileManagementSystem.Domain.Entities;
using FileManagementSystem.Infrastructure.Data;

namespace FileManagementSystem.Infrastructure.Repositories;

public class FileRepository : Repository<FileItem>, IFileRepository
{
    public FileRepository(AppDbContext context) : base(context)
    {
    }
    
    public async Task<FileItem?> GetByHashAsync(byte[] hash, CancellationToken cancellationToken = default)
    {
        var hashHex = Convert.ToHexString(hash);
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.HashHex == hashHex, cancellationToken);
    }
    
    public async Task<IReadOnlyList<FileItem>> SearchAsync(
        string? searchTerm,
        List<string>? tags,
        bool? isPhoto,
        Guid? folderId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(f => 
                f.Path.ToLower().Contains(term) ||
                f.MimeType.ToLower().Contains(term) ||
                (f.CameraMake != null && f.CameraMake.ToLower().Contains(term)) ||
                (f.CameraModel != null && f.CameraModel.ToLower().Contains(term)));
        }
        
        if (tags != null && tags.Any())
        {
            query = query.Where(f => tags.All(tag => f.Tags.Contains(tag)));
        }
        
        if (isPhoto.HasValue)
        {
            query = query.Where(f => f.IsPhoto == isPhoto.Value);
        }
        
        if (folderId.HasValue)
        {
            query = query.Where(f => f.FolderId == folderId.Value);
        }
        
        return await query
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<int> CountAsync(
        string? searchTerm,
        List<string>? tags,
        bool? isPhoto,
        Guid? folderId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(f => 
                f.Path.ToLower().Contains(term) ||
                f.MimeType.ToLower().Contains(term) ||
                (f.CameraMake != null && f.CameraMake.ToLower().Contains(term)) ||
                (f.CameraModel != null && f.CameraModel.ToLower().Contains(term)));
        }
        
        if (tags != null && tags.Any())
        {
            query = query.Where(f => tags.All(tag => f.Tags.Contains(tag)));
        }
        
        if (isPhoto.HasValue)
        {
            query = query.Where(f => f.IsPhoto == isPhoto.Value);
        }
        
        if (folderId.HasValue)
        {
            query = query.Where(f => f.FolderId == folderId.Value);
        }
        
        return await query.CountAsync(cancellationToken);
    }
}

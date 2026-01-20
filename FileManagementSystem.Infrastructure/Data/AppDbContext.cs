using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using FileManagementSystem.Domain.Entities;

namespace FileManagementSystem.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    
    public DbSet<FileItem> FileItems { get; set; } = null!;
    public DbSet<Folder> Folders { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // FileItem configuration
        modelBuilder.Entity<FileItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Path).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.FileName).IsRequired(false).HasMaxLength(500); // Original filename - nullable to handle existing records
            entity.Property(e => e.IsCompressed).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.MimeType).HasMaxLength(255);
            entity.Property(e => e.CameraMake).HasMaxLength(100);
            entity.Property(e => e.CameraModel).HasMaxLength(100);
            entity.Property(e => e.ThumbnailPath).HasMaxLength(1000);
            
            // Store hash as string for better indexing and comparison
            entity.Property(e => e.HashHex)
                .IsRequired()
                .HasMaxLength(64); // SHA256 hex string length
            
            // Ignore the byte[] Hash property for EF - we'll use HashHex
            entity.Ignore(e => e.Hash);
            
            entity.HasIndex(e => e.HashHex);
            entity.HasIndex(e => e.Path).IsUnique();
            entity.HasIndex(e => new { e.IsPhoto, e.FolderId });
            entity.HasIndex(e => e.MimeType);
            entity.HasIndex(e => e.CreatedDate);
            
            entity.HasOne(e => e.Folder)
                .WithMany(f => f.Files)
                .HasForeignKey(e => e.FolderId)
                .OnDelete(DeleteBehavior.SetNull);
            
            // Store Tags as JSON in SQLite
            entity.Property(e => e.Tags)
                .HasConversion(
                    v => string.Join(";", v),
                    v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList())
                .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c != null ? c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())) : 0,
                    c => c != null ? c.ToList() : new List<string>()));
        });
        
        // Folder configuration
        modelBuilder.Entity<Folder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Path).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
            
            entity.HasIndex(e => e.Path).IsUnique();
            
            entity.HasOne(e => e.ParentFolder)
                .WithMany(f => f.SubFolders)
                .HasForeignKey(e => e.ParentFolderId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        
        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
            
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            
            // Store Roles as JSON in SQLite
            entity.Property(e => e.Roles)
                .HasConversion(
                    v => string.Join(";", v),
                    v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList())
                .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                    (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                    c => c != null ? c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())) : 0,
                    c => c != null ? c.ToList() : new List<string>()));
        });
    }
}

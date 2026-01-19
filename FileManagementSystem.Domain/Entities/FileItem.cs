namespace FileManagementSystem.Domain.Entities;

public class FileItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public required string Path { get; set; }
    
    public byte[] Hash { get; set; } = Array.Empty<byte>();
    
    // Computed property for EF Core - stored as string in database
    public string HashHex { get; set; } = string.Empty;
    
    public long Size { get; set; }
    
    public string MimeType { get; set; } = string.Empty;
    
    public List<string> Tags { get; set; } = new();
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    
    public bool IsPhoto { get; set; }
    
    // Photo metadata (EXIF)
    public DateTime? PhotoDateTaken { get; set; }
    
    public string? CameraMake { get; set; }
    
    public string? CameraModel { get; set; }
    
    public double? Latitude { get; set; }
    
    public double? Longitude { get; set; }
    
    // Thumbnail path (generated)
    public string? ThumbnailPath { get; set; }
    
    // Navigation properties
    public Guid? FolderId { get; set; }
    
    public Folder? Folder { get; set; }
}

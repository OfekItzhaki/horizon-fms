namespace FileManagementSystem.Application.DTOs;

public record FileItemDto
{
    public Guid Id { get; init; }
    public string Path { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty; // Original filename for display
    public string HashHex { get; init; } = string.Empty;
    public long Size { get; init; }
    public bool IsCompressed { get; init; }
    public string MimeType { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = new();
    public DateTime CreatedDate { get; init; }
    public bool IsPhoto { get; init; }
    public DateTime? PhotoDateTaken { get; init; }
    public string? CameraMake { get; init; }
    public string? CameraModel { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? ThumbnailPath { get; init; }
    public Guid? FolderId { get; init; }
    public string? FolderPath { get; init; }
}

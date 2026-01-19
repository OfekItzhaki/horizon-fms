namespace FileManagementSystem.Application.Interfaces;

public record PhotoMetadata
{
    public DateTime? DateTaken { get; init; }
    public string? CameraMake { get; init; }
    public string? CameraModel { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
}

public interface IMetadataService
{
    Task<PhotoMetadata?> ExtractPhotoMetadataAsync(string filePath, CancellationToken cancellationToken = default);
    Task<bool> IsPhotoFileAsync(string filePath, CancellationToken cancellationToken = default);
}

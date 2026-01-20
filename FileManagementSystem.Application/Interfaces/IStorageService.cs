namespace FileManagementSystem.Application.Interfaces;

public record FileData(byte[] Content, string OriginalFileName, bool WasCompressed);

public interface IStorageService
{
    Task<string> SaveFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default);
    Task<FileData> ReadFileAsync(string filePath, bool isCompressed, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string filePath, bool moveToRecycleBin = true, CancellationToken cancellationToken = default);
    Task<string> GenerateThumbnailAsync(string imagePath, int maxWidth, int maxHeight, CancellationToken cancellationToken = default);
    Task<byte[]> ComputeHashAsync(string filePath, CancellationToken cancellationToken = default);
}

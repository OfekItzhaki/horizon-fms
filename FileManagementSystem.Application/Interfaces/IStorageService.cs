namespace FileManagementSystem.Application.Interfaces;

public interface IStorageService
{
    Task<string> SaveFileAsync(string sourcePath, string destinationPath, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string filePath, bool moveToRecycleBin = true, CancellationToken cancellationToken = default);
    Task<string> GenerateThumbnailAsync(string imagePath, int maxWidth, int maxHeight, CancellationToken cancellationToken = default);
    Task<byte[]> ComputeHashAsync(string filePath, CancellationToken cancellationToken = default);
}

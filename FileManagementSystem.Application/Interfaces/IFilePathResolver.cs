namespace FileManagementSystem.Application.Interfaces;

public interface IFilePathResolver
{
    string? ResolveFilePath(string storedPath, bool isCompressed);
}

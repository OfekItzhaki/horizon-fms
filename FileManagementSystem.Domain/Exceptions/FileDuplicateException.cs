namespace FileManagementSystem.Domain.Exceptions;

public class FileDuplicateException : Exception
{
    public string FilePath { get; }
    
    public byte[] Hash { get; }
    
    public FileDuplicateException(string filePath, byte[] hash)
        : base($"File with hash {Convert.ToHexString(hash)} already exists: {filePath}")
    {
        FilePath = filePath;
        Hash = hash;
    }
    
    public FileDuplicateException(string filePath, byte[] hash, Exception innerException)
        : base($"File with hash {Convert.ToHexString(hash)} already exists: {filePath}", innerException)
    {
        FilePath = filePath;
        Hash = hash;
    }
}

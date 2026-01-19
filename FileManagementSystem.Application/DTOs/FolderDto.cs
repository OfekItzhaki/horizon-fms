namespace FileManagementSystem.Application.DTOs;

public record FolderDto
{
    public Guid Id { get; init; }
    public string Path { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public Guid? ParentFolderId { get; init; }
    public DateTime CreatedDate { get; init; }
    public int FileCount { get; init; }
    public int SubFolderCount { get; init; }
}

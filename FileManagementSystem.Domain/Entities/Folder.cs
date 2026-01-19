namespace FileManagementSystem.Domain.Entities;

public class Folder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public required string Path { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public Guid? ParentFolderId { get; set; }
    
    public Folder? ParentFolder { get; set; }
    
    public List<Folder> SubFolders { get; set; } = new();
    
    public List<FileItem> Files { get; set; } = new();
    
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

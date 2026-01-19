using FileManagementSystem.Application.DTOs;
using System.Collections.ObjectModel;

namespace FileManagementSystem.Presentation.ViewModels;

public class FolderViewModel
{
    public Guid Id { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid? ParentFolderId { get; set; }
    public ObservableCollection<FolderViewModel> SubFolders { get; set; } = new();
    
    public FolderViewModel(FolderDto folder)
    {
        Id = folder.Id;
        Path = folder.Path;
        Name = folder.Name;
        ParentFolderId = folder.ParentFolderId;
    }
}

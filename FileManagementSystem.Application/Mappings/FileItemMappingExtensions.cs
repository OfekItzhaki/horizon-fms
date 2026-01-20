using System.IO;
using FileManagementSystem.Domain.Entities;
using FileManagementSystem.Application.DTOs;

namespace FileManagementSystem.Application.Mappings;

public static class FileItemMappingExtensions
{
    public static FileItemDto ToDto(this FileItem fileItem)
    {
        return new FileItemDto
        {
            Id = fileItem.Id,
            Path = fileItem.Path,
            FileName = !string.IsNullOrEmpty(fileItem.FileName) 
                ? fileItem.FileName 
                : (!string.IsNullOrEmpty(fileItem.Path) 
                    ? Path.GetFileName(fileItem.Path) 
                    : string.Empty),
            HashHex = !string.IsNullOrEmpty(fileItem.HashHex) ? fileItem.HashHex : Convert.ToHexString(fileItem.Hash),
            Size = fileItem.Size,
            IsCompressed = fileItem.IsCompressed,
            MimeType = fileItem.MimeType,
            Tags = fileItem.Tags.ToList(),
            CreatedDate = fileItem.CreatedDate,
            IsPhoto = fileItem.IsPhoto,
            PhotoDateTaken = fileItem.PhotoDateTaken,
            CameraMake = fileItem.CameraMake,
            CameraModel = fileItem.CameraModel,
            Latitude = fileItem.Latitude,
            Longitude = fileItem.Longitude,
            ThumbnailPath = fileItem.ThumbnailPath,
            FolderId = fileItem.FolderId,
            FolderPath = fileItem.Folder?.Path
        };
    }
    
    public static FolderDto ToDto(this Folder folder, int fileCount = 0, int subFolderCount = 0)
    {
        return new FolderDto
        {
            Id = folder.Id,
            Path = folder.Path,
            Name = folder.Name,
            ParentFolderId = folder.ParentFolderId,
            CreatedDate = folder.CreatedDate,
            FileCount = fileCount,
            SubFolderCount = subFolderCount
        };
    }
}

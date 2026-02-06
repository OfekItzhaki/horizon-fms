import type {
  FileItemDto as BaseFileItemDto,
  FolderDto as BaseFolderDto,
  SearchFilesResult,
  GetFoldersResult,
  CreateFolderRequest,
  CreateFolderResult,
  RenameFileRequest,
  RenameFileResult,
  RenameFolderRequest,
  RenameFolderResult,
  DeleteFolderResult,
  AddTagsRequest,
  UploadFileResult
} from '../services/api-client';

export type {
  BaseFileItemDto as FileItemDto,
  SearchFilesResult,
  GetFoldersResult,
  CreateFolderRequest,
  CreateFolderResult,
  RenameFileRequest,
  RenameFileResult,
  RenameFolderRequest,
  RenameFolderResult,
  DeleteFolderResult,
  AddTagsRequest,
  UploadFileResult
};

export interface FolderDto extends BaseFolderDto {
  subFolders?: FolderDto[];
}

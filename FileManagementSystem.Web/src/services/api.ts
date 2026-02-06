import axios from 'axios';
import type { FileItemDto, FolderDto, SearchFilesResult, GetFoldersResult } from '../types';

// Base URL for the API, defaults to Vite proxy in dev
const API_BASE_URL = import.meta.env.VITE_API_URL || '/api';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Re-export types for convenience
export type { FileItemDto, FolderDto, SearchFilesResult, GetFoldersResult };

// API methods
export const fileApi = {
  getFiles: async (params?: {
    searchTerm?: string;
    tags?: string[];
    isPhoto?: boolean;
    folderId?: string;
    skip?: number;
    take?: number;
  }): Promise<SearchFilesResult> => {
    const response = await apiClient.get<SearchFilesResult>('/files', { params });
    return response.data;
  },

  getFile: async (id: string): Promise<FileItemDto> => {
    const response = await apiClient.get<FileItemDto>(`/files/${id}`);
    return response.data;
  },

  uploadFile: async (file: File, destinationFolderId?: string): Promise<any> => {
    const formData = new FormData();
    formData.append('file', file);
    if (destinationFolderId) {
      formData.append('destinationFolderId', destinationFolderId);
    }
    const response = await apiClient.post('/files/upload', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  },

  deleteFile: async (id: string, moveToRecycleBin: boolean = true): Promise<void> => {
    await apiClient.delete(`/files/${id}`, { params: { moveToRecycleBin } });
  },

  renameFile: async (id: string, newName: string): Promise<any> => {
    const response = await apiClient.put(`/files/${id}/rename`, { newName });
    return response.data;
  },

  addTags: async (id: string, tags: string[]): Promise<void> => {
    await apiClient.post(`/files/${id}/tags`, { tags });
  },

  downloadFile: async (id: string): Promise<string> => {
    return `${API_BASE_URL}/files/${id}/download`;
  },
};

export const folderApi = {
  getFolders: async (parentFolderId?: string): Promise<GetFoldersResult> => {
    const params = parentFolderId ? { parentFolderId } : {};
    const response = await apiClient.get<GetFoldersResult>('/folders', { params });
    return response.data;
  },

  createFolder: async (name: string, parentFolderId?: string): Promise<any> => {
    const response = await apiClient.post('/folders', { 
      name, 
      parentFolderId: parentFolderId || null 
    });
    return response.data;
  },

  renameFolder: async (id: string, newName: string): Promise<any> => {
    const response = await apiClient.put(`/folders/${id}/rename`, { newName });
    return response.data;
  },

  deleteFolder: async (id: string, deleteFiles: boolean = false): Promise<void> => {
    await apiClient.delete(`/folders/${id}`, { 
      params: { deleteFiles } 
    });
  },
};

export default apiClient;

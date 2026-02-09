import axios from 'axios';
import type { FileItemDto, FolderDto, SearchFilesResult, GetFoldersResult } from '../types';

// Base URL for the API, defaults to Vite proxy in dev
const API_BASE_URL = import.meta.env.VITE_API_URL || '/api/v1';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Add a response interceptor for debugging 400 errors
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response) {
      // The request was made and the server responded with a status code
      // that falls out of the range of 2xx
      console.error('API Error Response:', {
        status: error.response.status,
        data: error.response.data,
        headers: error.response.headers,
        url: error.config?.url,
      });
    } else if (error.request) {
      // The request was made but no response was received
      console.error('API No Response:', error.request);
    } else {
      // Something happened in setting up the request that triggered an Error
      console.error('API Error Message:', error.message);
    }
    return Promise.reject(error);
  },
);

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

  uploadFile: async (file: File, destinationFolderId?: string): Promise<FileItemDto> => {
    const formData = new FormData();
    formData.append('file', file);
    if (destinationFolderId) {
      formData.append('destinationFolderId', destinationFolderId);
    }
    const response = await apiClient.post<FileItemDto>('/files/upload', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  },

  deleteFile: async (id: string, moveToRecycleBin: boolean = true): Promise<void> => {
    await apiClient.delete(`/files/${id}`, { params: { moveToRecycleBin } });
  },

  renameFile: async (id: string, newName: string): Promise<FileItemDto> => {
    const response = await apiClient.put<FileItemDto>(`/files/${id}/rename`, { newName });
    return response.data;
  },

  addTags: async (id: string, tags: string[]): Promise<void> => {
    await apiClient.post(`/files/${id}/tags`, { tags });
  },

  setTags: async (id: string, tags: string[]): Promise<void> => {
    await apiClient.put(`/files/${id}/tags`, { tags });
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

  createFolder: async (name: string, parentFolderId?: string): Promise<FolderDto> => {
    const response = await apiClient.post<FolderDto>('/folders', {
      name,
      parentFolderId: parentFolderId || null,
    });
    return response.data;
  },

  renameFolder: async (id: string, newName: string): Promise<FolderDto> => {
    const response = await apiClient.put<FolderDto>(`/folders/${id}/rename`, { newName });
    return response.data;
  },

  deleteFolder: async (id: string, deleteFiles: boolean = false): Promise<void> => {
    await apiClient.delete(`/folders/${id}`, {
      params: { deleteFiles },
    });
  },
};

export default apiClient;

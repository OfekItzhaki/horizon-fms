import { useState, useEffect, memo } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'react-hot-toast';
import { folderApi } from '../services/api';
import type { FolderDto } from '../types';
import { FolderItem } from './FolderItem';
import { CreateFolderInput } from './CreateFolderInput';
import { LoadingSpinner } from './LoadingSpinner';
import './FolderTree.css';

interface FolderTreeProps {
  folders: FolderDto[];
  onFolderSelect: (folderId: string | undefined) => void;
  selectedFolderId?: string;
  isLoading?: boolean;
}

const FolderTree = memo(
  ({ folders, onFolderSelect, selectedFolderId, isLoading }: FolderTreeProps) => {
    const queryClient = useQueryClient();
    const [showCreateInput, setShowCreateInput] = useState(false);
    const [isMobile, setIsMobile] = useState(window.innerWidth <= 768);

    useEffect(() => {
      const handleResize = () => setIsMobile(window.innerWidth <= 768);
      window.addEventListener('resize', handleResize);
      return () => window.removeEventListener('resize', handleResize);
    }, []);

    const createMutation = useMutation({
      mutationFn: (name: string) => folderApi.createFolder(name),
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: ['folders'] });
        setShowCreateInput(false);
        toast.success('Folder created successfully');
      },
      onError: (error: Error) => {
        toast.error(`Failed to create folder: ${error.message || 'Unknown error'}`);
      },
    });

    const renameMutation = useMutation({
      mutationFn: ({ id, newName }: { id: string; newName: string }) =>
        folderApi.renameFolder(id, newName),
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: ['folders'] });
        queryClient.invalidateQueries({ queryKey: ['files'] });
        toast.success('Folder renamed successfully');
      },
      onError: (error: Error & { response?: { data?: { detail?: string } } }) => {
        // Check for specific error message from backend
        const errorMessage =
          error.response?.data?.detail || error.message || 'Failed to rename folder';

        // Special handling for Default folder protection
        if (errorMessage.includes('Default folder')) {
          toast.error(errorMessage, { duration: 5000, icon: '🛡️' });
        } else {
          toast.error(errorMessage);
        }
      },
    });

    const deleteMutation = useMutation({
      mutationFn: ({ id, deleteFiles }: { id: string; deleteFiles: boolean }) =>
        folderApi.deleteFolder(id, deleteFiles),
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: ['folders'] });
        queryClient.invalidateQueries({ queryKey: ['files'] });
        if (selectedFolderId) onFolderSelect(undefined);
        toast.success('Folder deleted successfully');
      },
      onError: (error: Error & { response?: { data?: { detail?: string } } }) => {
        // Check for specific error message from backend
        const errorMessage =
          error.response?.data?.detail || error.message || 'Failed to delete folder';

        // Special handling for Default folder protection
        if (errorMessage.includes('Default folder')) {
          toast.error(errorMessage, { duration: 5000, icon: '🛡️' });
        } else {
          toast.error(errorMessage);
        }
      },
    });

    const handleDeleteFolder = (folder: FolderDto) => {
      const hasContent = (folder.fileCount ?? 0) > 0 || (folder.subFolderCount ?? 0) > 0;
      const message = hasContent
        ? `Delete folder "${folder.name}" and all its content?`
        : `Delete folder "${folder.name}"?`;

      if (window.confirm(message)) {
        deleteMutation.mutate({ id: folder.id!, deleteFiles: true });
      }
    };

    const renderFolders = (folderList: FolderDto[], level: number = 0) => {
      return folderList.map((folder) => (
        <div key={folder.id}>
          <FolderItem
            folder={folder}
            level={level}
            isSelected={folder.id === selectedFolderId}
            isMobile={isMobile}
            onSelect={(id) => onFolderSelect(id)}
            onRename={(id, newName) => renameMutation.mutate({ id, newName })}
            onDelete={handleDeleteFolder}
            isDeleting={deleteMutation.isPending}
          />
          {folder.subFolders &&
            folder.subFolders.length > 0 &&
            renderFolders(folder.subFolders, level + 1)}
        </div>
      ));
    };

    return (
      <div className='folder-tree-container'>
        <div className='folder-tree-header'>
          <h3 className='folder-tree-title'>Folders</h3>
          <button className='new-folder-btn' onClick={() => setShowCreateInput(true)}>
            + New
          </button>
        </div>

        {showCreateInput && (
          <CreateFolderInput
            onSave={(name) => createMutation.mutate(name)}
            onCancel={() => setShowCreateInput(false)}
            isPending={createMutation.isPending}
          />
        )}

        <div
          className={`folder-item ${selectedFolderId === undefined ? 'selected' : ''}`}
          onClick={() => onFolderSelect(undefined)}
        >
          <span className='folder-name'>📂 All Files</span>
        </div>

        {isLoading ? (
          <div className='flex justify-center py-4'>
            <LoadingSpinner size='sm' color='#4dabf7' />
          </div>
        ) : (
          renderFolders(folders)
        )}
      </div>
    );
  },
);

FolderTree.displayName = 'FolderTree';

export default FolderTree;

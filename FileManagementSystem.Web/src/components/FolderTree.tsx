import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { folderApi } from '../services/api';
import type { FolderDto } from '../types';

interface FolderTreeProps {
  folders: FolderDto[];
  onFolderSelect: (folderId: string | undefined) => void;
  selectedFolderId?: string;
}

const FolderTree = ({ folders, onFolderSelect, selectedFolderId }: FolderTreeProps) => {
  const queryClient = useQueryClient();
  const [editingFolderId, setEditingFolderId] = useState<string | null>(null);
  const [editingName, setEditingName] = useState('');
  const [showCreateInput, setShowCreateInput] = useState(false);
  const [newFolderName, setNewFolderName] = useState('');
  const [hoveredFolderId, setHoveredFolderId] = useState<string | null>(null);

  const createMutation = useMutation({
    mutationFn: async (name: string) => {
      return await folderApi.createFolder(name);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['folders'] });
      setShowCreateInput(false);
      setNewFolderName('');
    },
    onError: (error: any) => {
      alert(`Failed to create folder: ${error.response?.data?.message || error.message || 'Unknown error'}`);
    },
  });

  const renameMutation = useMutation({
    mutationFn: async ({ id, newName }: { id: string; newName: string }) => {
      return await folderApi.renameFolder(id, newName);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['folders'] });
      queryClient.invalidateQueries({ queryKey: ['files'] });
      setEditingFolderId(null);
      setEditingName('');
    },
    onError: (error: any) => {
      alert(`Failed to rename folder: ${error.response?.data?.errorMessage || error.response?.data?.message || error.message || 'Unknown error'}`);
      setEditingFolderId(null);
      setEditingName('');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: async ({ id, deleteFiles }: { id: string; deleteFiles: boolean }) => {
      return await folderApi.deleteFolder(id, deleteFiles);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['folders'] });
      queryClient.invalidateQueries({ queryKey: ['files'] });
      if (selectedFolderId) {
        onFolderSelect(undefined);
      }
    },
    onError: (error: any) => {
      alert(`Failed to delete folder: ${error.response?.data?.errorMessage || error.response?.data?.message || error.message || 'Unknown error'}`);
    },
  });

  const handleCreateFolder = () => {
    if (newFolderName.trim()) {
      // Create folder with no parent (root level) - pass undefined instead of selectedFolderId
      createMutation.mutate(newFolderName.trim());
    }
  };

  const handleStartRename = (folder: FolderDto) => {
    setEditingFolderId(folder.id);
    setEditingName(folder.name);
  };

  const handleSaveRename = (folderId: string) => {
    if (editingName.trim()) {
      renameMutation.mutate({ id: folderId, newName: editingName.trim() });
    } else {
      setEditingFolderId(null);
      setEditingName('');
    }
  };

  const handleDeleteFolder = (folder: FolderDto) => {
    if (folder.fileCount > 0 || folder.subFolderCount > 0) {
      const message = `Delete folder "${folder.name}"?${folder.fileCount > 0 ? `\nThis folder contains ${folder.fileCount} file(s).` : ''}${folder.subFolderCount > 0 ? `\nThis folder contains ${folder.subFolderCount} subfolder(s).` : ''}\n\nFiles and subfolders will be deleted.`;
      if (window.confirm(message)) {
        deleteMutation.mutate({ id: folder.id, deleteFiles: true });
      }
    } else {
      if (window.confirm(`Delete folder "${folder.name}"?`)) {
        deleteMutation.mutate({ id: folder.id, deleteFiles: false });
      }
    }
  };

  const renderFolder = (folder: FolderDto, level: number = 0) => {
    const isSelected = folder.id === selectedFolderId;
    const isEditing = editingFolderId === folder.id;
    const isHovered = hoveredFolderId === folder.id;

    return (
      <div key={folder.id}>
        <div
          onMouseEnter={() => setHoveredFolderId(folder.id)}
          onMouseLeave={() => setHoveredFolderId(null)}
          style={{
            padding: '0.5rem',
            paddingLeft: `${level * 1.5 + 0.5}rem`,
            cursor: 'pointer',
            background: isSelected ? '#e3f2fd' : 'transparent',
            borderRadius: '4px',
            marginBottom: '2px',
            display: 'flex',
            alignItems: 'center',
            gap: '0.5rem',
            position: 'relative',
          }}
        >
          {isEditing ? (
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.25rem', flex: 1 }}>
              <input
                type="text"
                value={editingName}
                onChange={(e) => setEditingName(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter') {
                    handleSaveRename(folder.id);
                  } else if (e.key === 'Escape') {
                    setEditingFolderId(null);
                    setEditingName('');
                  }
                }}
                autoFocus
                style={{
                  padding: '0.25rem',
                  fontSize: '0.875rem',
                  border: '1px solid #007bff',
                  borderRadius: '4px',
                  flex: 1,
                }}
              />
              <button
                onClick={() => handleSaveRename(folder.id)}
                style={{
                  padding: '0.25rem 0.5rem',
                  fontSize: '0.75rem',
                  background: '#28a745',
                  color: '#fff',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: 'pointer',
                }}
              >
                ✓
              </button>
              <button
                onClick={() => {
                  setEditingFolderId(null);
                  setEditingName('');
                }}
                style={{
                  padding: '0.25rem 0.5rem',
                  fontSize: '0.75rem',
                  background: '#dc3545',
                  color: '#fff',
                  border: 'none',
                  borderRadius: '4px',
                  cursor: 'pointer',
                }}
              >
                ✕
              </button>
            </div>
          ) : (
            <>
              <span
                onClick={() => onFolderSelect(folder.id)}
                style={{ flex: 1 }}
              >
                {folder.name}
                {(folder.fileCount > 0 || folder.subFolderCount > 0) && (
                  <span style={{ fontSize: '0.75rem', color: '#666', marginLeft: '0.5rem' }}>
                    ({folder.fileCount + folder.subFolderCount})
                  </span>
                )}
              </span>
              {isHovered && (
                <div style={{ display: 'flex', gap: '0.25rem' }}>
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      handleStartRename(folder);
                    }}
                    title="Rename folder"
                    style={{
                      padding: '0.25rem 0.5rem',
                      fontSize: '0.75rem',
                      background: '#ffc107',
                      color: '#000',
                      border: 'none',
                      borderRadius: '4px',
                      cursor: 'pointer',
                    }}
                  >
                    ✏️
                  </button>
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      handleDeleteFolder(folder);
                    }}
                    title="Delete folder"
                    disabled={deleteMutation.isPending}
                    style={{
                      padding: '0.25rem 0.5rem',
                      fontSize: '0.75rem',
                      background: '#dc3545',
                      color: '#fff',
                      border: 'none',
                      borderRadius: '4px',
                      cursor: deleteMutation.isPending ? 'not-allowed' : 'pointer',
                      opacity: deleteMutation.isPending ? 0.6 : 1,
                    }}
                  >
                    🗑️
                  </button>
                </div>
              )}
            </>
          )}
        </div>
        {folder.subFolders?.map((subFolder) => renderFolder(subFolder, level + 1))}
      </div>
    );
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '0.5rem' }}>
        <h3 style={{ margin: 0 }}>Folders</h3>
        <button
          onClick={() => setShowCreateInput(true)}
          style={{
            padding: '0.25rem 0.5rem',
            fontSize: '0.75rem',
            background: '#007bff',
            color: '#fff',
            border: 'none',
            borderRadius: '4px',
            cursor: 'pointer',
          }}
          title="Create new folder"
        >
          + New
        </button>
      </div>
      
      {showCreateInput && (
        <div style={{ marginBottom: '0.5rem', padding: '0.5rem', background: '#f0f0f0', borderRadius: '4px' }}>
          <input
            type="text"
            placeholder="Folder name"
            value={newFolderName}
            onChange={(e) => setNewFolderName(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === 'Enter') {
                handleCreateFolder();
              } else if (e.key === 'Escape') {
                setShowCreateInput(false);
                setNewFolderName('');
              }
            }}
            autoFocus
            style={{
              width: '100%',
              padding: '0.25rem',
              fontSize: '0.875rem',
              border: '1px solid #ccc',
              borderRadius: '4px',
              marginBottom: '0.25rem',
            }}
          />
          <div style={{ display: 'flex', gap: '0.25rem' }}>
            <button
              onClick={handleCreateFolder}
              disabled={createMutation.isPending || !newFolderName.trim()}
              style={{
                padding: '0.25rem 0.5rem',
                fontSize: '0.75rem',
                background: '#28a745',
                color: '#fff',
                border: 'none',
                borderRadius: '4px',
                cursor: createMutation.isPending || !newFolderName.trim() ? 'not-allowed' : 'pointer',
                opacity: createMutation.isPending || !newFolderName.trim() ? 0.6 : 1,
              }}
            >
              Create
            </button>
            <button
              onClick={() => {
                setShowCreateInput(false);
                setNewFolderName('');
              }}
              style={{
                padding: '0.25rem 0.5rem',
                fontSize: '0.75rem',
                background: '#6c757d',
                color: '#fff',
                border: 'none',
                borderRadius: '4px',
                cursor: 'pointer',
              }}
            >
              Cancel
            </button>
          </div>
        </div>
      )}

      <div
        onClick={() => onFolderSelect(undefined)}
        style={{
          padding: '0.5rem',
          cursor: 'pointer',
          background: selectedFolderId === undefined ? '#e3f2fd' : 'transparent',
          borderRadius: '4px',
          marginBottom: '8px',
          fontWeight: 'bold',
        }}
      >
        All Files
      </div>
      {folders.map((folder) => renderFolder(folder))}
    </div>
  );
};

export default FolderTree;

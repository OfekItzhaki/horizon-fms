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
            padding: '0.75rem 1rem',
            paddingLeft: `${level * 1.5 + 1}rem`,
            cursor: 'pointer',
            background: isSelected 
              ? 'linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%)' 
              : 'transparent',
            borderRadius: '8px',
            marginBottom: '4px',
            display: 'flex',
            alignItems: 'center',
            gap: '0.5rem',
            position: 'relative',
            border: isSelected ? '1px solid rgba(102, 126, 234, 0.2)' : '1px solid transparent',
            transition: 'all 0.2s'
          }}
          onMouseEnter={(e) => {
            if (!isSelected) {
              e.currentTarget.style.background = '#f8fafc';
            }
          }}
          onMouseLeave={(e) => {
            if (!isSelected) {
              e.currentTarget.style.background = 'transparent';
            }
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
                  padding: '0.5rem 0.75rem',
                  fontSize: '0.9rem',
                  border: '1px solid #667eea',
                  borderRadius: '8px',
                  flex: 1,
                  outline: 'none',
                  transition: 'all 0.2s',
                  backgroundColor: '#ffffff',
                  boxShadow: '0 0 0 3px rgba(102, 126, 234, 0.1)'
                }}
              />
              <button
                onClick={() => handleSaveRename(folder.id)}
                style={{
                  padding: '0.5rem 0.75rem',
                  fontSize: '0.875rem',
                  background: 'linear-gradient(135deg, #10b981 0%, #059669 100%)',
                  color: '#fff',
                  border: 'none',
                  borderRadius: '6px',
                  cursor: 'pointer',
                  fontWeight: '600',
                  transition: 'all 0.2s',
                  boxShadow: '0 2px 4px rgba(16, 185, 129, 0.3)'
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.transform = 'translateY(-1px)';
                  e.currentTarget.style.boxShadow = '0 4px 6px rgba(16, 185, 129, 0.4)';
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.transform = 'translateY(0)';
                  e.currentTarget.style.boxShadow = '0 2px 4px rgba(16, 185, 129, 0.3)';
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
                  padding: '0.5rem 0.75rem',
                  fontSize: '0.875rem',
                  background: 'linear-gradient(135deg, #ef4444 0%, #dc2626 100%)',
                  color: '#fff',
                  border: 'none',
                  borderRadius: '6px',
                  cursor: 'pointer',
                  fontWeight: '600',
                  transition: 'all 0.2s',
                  boxShadow: '0 2px 4px rgba(239, 68, 68, 0.3)'
                }}
                onMouseEnter={(e) => {
                  e.currentTarget.style.transform = 'translateY(-1px)';
                  e.currentTarget.style.boxShadow = '0 4px 6px rgba(239, 68, 68, 0.4)';
                }}
                onMouseLeave={(e) => {
                  e.currentTarget.style.transform = 'translateY(0)';
                  e.currentTarget.style.boxShadow = '0 2px 4px rgba(239, 68, 68, 0.3)';
                }}
              >
                ✕
              </button>
            </div>
          ) : (
            <>
              <span
                onClick={() => onFolderSelect(folder.id)}
                style={{ 
                  flex: 1,
                  fontSize: '0.95rem',
                  fontWeight: isSelected ? '600' : '500',
                  color: isSelected ? '#667eea' : '#475569',
                  transition: 'all 0.2s'
                }}
              >
                📁 {folder.name}
                {(folder.fileCount > 0 || folder.subFolderCount > 0) && (
                  <span style={{ 
                    fontSize: '0.8rem', 
                    color: '#94a3b8', 
                    marginLeft: '0.5rem',
                    fontWeight: '500'
                  }}>
                    ({folder.fileCount + folder.subFolderCount})
                  </span>
                )}
              </span>
              {isHovered && (
                <div style={{ display: 'flex', gap: '0.375rem' }}>
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      handleStartRename(folder);
                    }}
                    title="Rename folder"
                    style={{
                      padding: '0.375rem 0.625rem',
                      fontSize: '0.875rem',
                      background: 'linear-gradient(135deg, #f59e0b 0%, #d97706 100%)',
                      color: '#fff',
                      border: 'none',
                      borderRadius: '6px',
                      cursor: 'pointer',
                      fontWeight: '600',
                      transition: 'all 0.2s',
                      boxShadow: '0 2px 4px rgba(245, 158, 11, 0.3)'
                    }}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.transform = 'translateY(-1px)';
                      e.currentTarget.style.boxShadow = '0 4px 6px rgba(245, 158, 11, 0.4)';
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.transform = 'translateY(0)';
                      e.currentTarget.style.boxShadow = '0 2px 4px rgba(245, 158, 11, 0.3)';
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
                      padding: '0.375rem 0.625rem',
                      fontSize: '0.875rem',
                      background: deleteMutation.isPending 
                        ? '#cbd5e1' 
                        : 'linear-gradient(135deg, #ef4444 0%, #dc2626 100%)',
                      color: '#fff',
                      border: 'none',
                      borderRadius: '6px',
                      cursor: deleteMutation.isPending ? 'not-allowed' : 'pointer',
                      opacity: deleteMutation.isPending ? 0.7 : 1,
                      fontWeight: '600',
                      transition: 'all 0.2s',
                      boxShadow: deleteMutation.isPending 
                        ? 'none' 
                        : '0 2px 4px rgba(239, 68, 68, 0.3)'
                    }}
                    onMouseEnter={(e) => {
                      if (!deleteMutation.isPending) {
                        e.currentTarget.style.transform = 'translateY(-1px)';
                        e.currentTarget.style.boxShadow = '0 4px 6px rgba(239, 68, 68, 0.4)';
                      }
                    }}
                    onMouseLeave={(e) => {
                      if (!deleteMutation.isPending) {
                        e.currentTarget.style.transform = 'translateY(0)';
                        e.currentTarget.style.boxShadow = '0 2px 4px rgba(239, 68, 68, 0.3)';
                      }
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
      <div style={{ 
        display: 'flex', 
        justifyContent: 'space-between', 
        alignItems: 'center', 
        marginBottom: '1rem' 
      }}>
        <h3 style={{ 
          margin: 0,
          fontSize: '1.25rem',
          fontWeight: '700',
          color: '#1e293b',
          letterSpacing: '-0.025em'
        }}>
          Folders
        </h3>
        <button
          onClick={() => setShowCreateInput(true)}
          style={{
            padding: '0.5rem 0.875rem',
            fontSize: '0.875rem',
            background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
            color: '#fff',
            border: 'none',
            borderRadius: '8px',
            cursor: 'pointer',
            fontWeight: '600',
            transition: 'all 0.2s',
            boxShadow: '0 2px 4px rgba(102, 126, 234, 0.3)'
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.transform = 'translateY(-1px)';
            e.currentTarget.style.boxShadow = '0 4px 6px rgba(102, 126, 234, 0.4)';
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.transform = 'translateY(0)';
            e.currentTarget.style.boxShadow = '0 2px 4px rgba(102, 126, 234, 0.3)';
          }}
          title="Create new folder"
        >
          + New
        </button>
      </div>
      
      {showCreateInput && (
        <div style={{ 
          marginBottom: '1rem', 
          padding: '1rem', 
          background: 'linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%)', 
          borderRadius: '10px',
          border: '1px solid #e2e8f0',
          boxShadow: '0 2px 4px rgba(0, 0, 0, 0.05)'
        }}>
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
              padding: '0.625rem 0.75rem',
              fontSize: '0.9rem',
              border: '1px solid #cbd5e1',
              borderRadius: '8px',
              marginBottom: '0.75rem',
              outline: 'none',
              transition: 'all 0.2s',
              backgroundColor: '#ffffff'
            }}
            onFocus={(e) => {
              e.target.style.borderColor = '#667eea';
              e.target.style.boxShadow = '0 0 0 3px rgba(102, 126, 234, 0.1)';
            }}
            onBlur={(e) => {
              e.target.style.borderColor = '#cbd5e1';
              e.target.style.boxShadow = 'none';
            }}
          />
          <div style={{ display: 'flex', gap: '0.5rem' }}>
            <button
              onClick={handleCreateFolder}
              disabled={createMutation.isPending || !newFolderName.trim()}
              style={{
                padding: '0.5rem 1rem',
                fontSize: '0.875rem',
                background: createMutation.isPending || !newFolderName.trim() 
                  ? '#cbd5e1' 
                  : 'linear-gradient(135deg, #10b981 0%, #059669 100%)',
                color: '#fff',
                border: 'none',
                borderRadius: '8px',
                cursor: createMutation.isPending || !newFolderName.trim() ? 'not-allowed' : 'pointer',
                opacity: createMutation.isPending || !newFolderName.trim() ? 0.7 : 1,
                fontWeight: '600',
                transition: 'all 0.2s',
                boxShadow: createMutation.isPending || !newFolderName.trim() 
                  ? 'none' 
                  : '0 2px 4px rgba(16, 185, 129, 0.3)'
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
                padding: '0.5rem 1rem',
                fontSize: '0.875rem',
                background: '#64748b',
                color: '#fff',
                border: 'none',
                borderRadius: '8px',
                cursor: 'pointer',
                fontWeight: '600',
                transition: 'all 0.2s'
              }}
              onMouseEnter={(e) => {
                e.currentTarget.style.background = '#475569';
              }}
              onMouseLeave={(e) => {
                e.currentTarget.style.background = '#64748b';
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
          padding: '0.75rem 1rem',
          cursor: 'pointer',
          background: selectedFolderId === undefined 
            ? 'linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%)' 
            : 'transparent',
          borderRadius: '8px',
          marginBottom: '0.75rem',
          fontWeight: '600',
          fontSize: '0.95rem',
          color: selectedFolderId === undefined ? '#667eea' : '#475569',
          border: selectedFolderId === undefined ? '1px solid rgba(102, 126, 234, 0.2)' : '1px solid transparent',
          transition: 'all 0.2s'
        }}
        onMouseEnter={(e) => {
          if (selectedFolderId !== undefined) {
            e.currentTarget.style.background = '#f1f5f9';
            e.currentTarget.style.color = '#667eea';
          }
        }}
        onMouseLeave={(e) => {
          if (selectedFolderId !== undefined) {
            e.currentTarget.style.background = 'transparent';
            e.currentTarget.style.color = '#475569';
          }
        }}
      >
        📂 All Files
      </div>
      {folders.map((folder) => renderFolder(folder))}
    </div>
  );
};

export default FolderTree;

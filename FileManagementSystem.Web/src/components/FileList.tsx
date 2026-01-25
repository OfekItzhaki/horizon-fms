import { useMutation, useQueryClient } from '@tanstack/react-query';
import { memo } from 'react';
import { fileApi } from '../services/api';
import type { FileItemDto } from '../types';

interface FileListProps {
  files: FileItemDto[];
  isLoading: boolean;
  totalCount: number;
}

const FileList = memo(({ files, isLoading, totalCount }: FileListProps) => {
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await fileApi.deleteFile(id, true); // Move to recycle bin
    },
    onSuccess: () => {
      // Invalidate and refetch files
      queryClient.invalidateQueries({ queryKey: ['files'] });
    },
  });

  const handleDelete = async (id: string, fileName: string) => {
    if (window.confirm(`Are you sure you want to delete "${fileName}"?`)) {
      try {
        await deleteMutation.mutateAsync(id);
      } catch (error) {
        console.error('Error deleting file:', error);
        alert(`Failed to delete ${fileName}`);
      }
    }
  };

  const handleOpen = async (file: FileItemDto) => {
    try {
      // Use the download endpoint to open the file directly
      const downloadUrl = await fileApi.downloadFile(file.id);
      
      // Open the file directly - browser will handle errors if file doesn't exist
      window.open(downloadUrl, '_blank');
    } catch (error) {
      console.error('Error opening file:', error);
      alert(`Unable to open file: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  };


  const formatSize = (bytes: number) => {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
  };

  // Clean filename: remove drive letters and full path, show only filename
  const getFileName = (path: string) => {
    if (!path) return '';
    
    // Remove drive letter (C:\, D:\, etc.) - case insensitive
    // Handles: C:\, C:/, c:\, c:/
    let normalized = path.replace(/^[A-Za-z]:[\\\/]?/i, '');
    
    // Normalize all backslashes to forward slashes
    normalized = normalized.replace(/\\/g, '/');
    
    // Remove leading slashes
    normalized = normalized.replace(/^\/+/, '');
    
    // Get just the filename (last part after /)
    const fileName = normalized.split('/').pop() || path;
    
    return fileName;
  };

  if (isLoading) {
    return <div>Loading files...</div>;
  }

  return (
    <div>
      <h2 style={{
        fontSize: '1.5rem',
        fontWeight: '700',
        color: '#1e293b',
        margin: '0 0 1.5rem 0',
        letterSpacing: '-0.025em'
      }}>
        Files <span style={{ color: '#64748b', fontWeight: '500', fontSize: '1.1rem' }}>({totalCount})</span>
      </h2>
      <div style={{ 
        overflowX: 'auto', 
        marginTop: '1rem', 
        borderRadius: '12px',
        boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)',
        backgroundColor: '#ffffff',
        overflow: 'hidden'
      }}>
        <table style={{ 
          width: '100%', 
          borderCollapse: 'collapse', 
          tableLayout: 'fixed',
          backgroundColor: '#fff',
          margin: 0
        }}>
            <colgroup>
              <col style={{ width: '30%' }} />
              <col style={{ width: '10%' }} />
              <col style={{ width: '15%' }} />
              <col style={{ width: '15%' }} />
              <col style={{ width: '15%' }} />
              <col style={{ width: '15%' }} />
            </colgroup>
            <thead>
              <tr style={{ 
                borderBottom: '2px solid #e2e8f0', 
                background: 'linear-gradient(to bottom, #f8fafc, #f1f5f9)'
              }}>
                <th style={{ 
                  textAlign: 'left', 
                  padding: '1.25rem 1rem', 
                  fontWeight: '600',
                  fontSize: '0.875rem',
                  color: '#475569',
                  textTransform: 'uppercase',
                  letterSpacing: '0.05em'
                }}>Name</th>
                <th style={{ 
                  textAlign: 'right', 
                  padding: '1.25rem 1rem', 
                  fontWeight: '600',
                  fontSize: '0.875rem',
                  color: '#475569',
                  textTransform: 'uppercase',
                  letterSpacing: '0.05em'
                }}>Size</th>
                <th style={{ 
                  textAlign: 'left', 
                  padding: '1.25rem 1rem', 
                  fontWeight: '600',
                  fontSize: '0.875rem',
                  color: '#475569',
                  textTransform: 'uppercase',
                  letterSpacing: '0.05em'
                }}>Type</th>
                <th style={{ 
                  textAlign: 'left', 
                  padding: '1.25rem 1rem', 
                  fontWeight: '600',
                  fontSize: '0.875rem',
                  color: '#475569',
                  textTransform: 'uppercase',
                  letterSpacing: '0.05em'
                }}>Tags</th>
                <th style={{ 
                  textAlign: 'left', 
                  padding: '1.25rem 1rem', 
                  fontWeight: '600',
                  fontSize: '0.875rem',
                  color: '#475569',
                  textTransform: 'uppercase',
                  letterSpacing: '0.05em'
                }}>Date</th>
                <th style={{ 
                  textAlign: 'center', 
                  padding: '1.25rem 1rem',
                  fontWeight: '600',
                  fontSize: '0.875rem',
                  color: '#475569',
                  textTransform: 'uppercase',
                  letterSpacing: '0.05em'
                }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {files.length === 0 && (
                <tr>
                  <td colSpan={6} style={{ 
                    padding: '3rem 1rem',
                    textAlign: 'center',
                    color: '#94a3b8',
                    fontSize: '1rem',
                    fontWeight: '500'
                  }}>
                    No files found
                  </td>
                </tr>
              )}
              {files.map((file, index) => {
                // Use FileName if available, otherwise extract from path
                const fileName = file.fileName || getFileName(file.path);
                
                return (
                  <tr 
                    key={file.id} 
                    style={{ 
                      borderBottom: '1px solid #f1f5f9',
                      backgroundColor: index % 2 === 0 ? '#ffffff' : '#f8fafc',
                      transition: 'all 0.2s ease',
                      height: 'auto'
                    }}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.backgroundColor = '#f0f9ff';
                      e.currentTarget.style.transform = 'scale(1.001)';
                      e.currentTarget.style.boxShadow = '0 4px 6px -1px rgba(0, 0, 0, 0.05)';
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.backgroundColor = index % 2 === 0 ? '#ffffff' : '#f8fafc';
                      e.currentTarget.style.transform = 'scale(1)';
                      e.currentTarget.style.boxShadow = 'none';
                    }}
                  >
                    <td style={{ 
                      padding: '0.75rem 1rem', 
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                      whiteSpace: 'nowrap',
                      fontSize: '0.95rem',
                      color: '#1e293b',
                      fontWeight: '500',
                      maxWidth: 0,
                      verticalAlign: 'middle'
                    }} title={fileName}>
                      {fileName}
                    </td>
                    <td style={{ 
                      textAlign: 'right', 
                      padding: '0.75rem 1rem', 
                      whiteSpace: 'nowrap',
                      fontSize: '0.9rem',
                      color: '#64748b',
                      fontWeight: '500',
                      verticalAlign: 'middle'
                    }}>
                      {formatSize(file.size)}
                    </td>
                    <td style={{ 
                      padding: '0.75rem 1rem', 
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                      whiteSpace: 'nowrap',
                      fontSize: '0.875rem',
                      color: '#64748b',
                      verticalAlign: 'middle'
                    }} title={file.mimeType}>
                      {file.mimeType || '-'}
                    </td>
                    <td style={{ 
                      padding: '0.75rem 1rem', 
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                      whiteSpace: 'nowrap',
                      fontSize: '0.875rem',
                      color: '#64748b',
                      verticalAlign: 'middle'
                    }} title={file.tags.join(', ') || ''}>
                      {file.tags.length > 0 ? file.tags.join(', ') : '-'}
                    </td>
                    <td style={{ 
                      padding: '0.75rem 1rem', 
                      whiteSpace: 'nowrap',
                      fontSize: '0.9rem',
                      color: '#64748b',
                      fontWeight: '500',
                      verticalAlign: 'middle'
                    }}>
                      {new Date(file.createdDate).toLocaleDateString()}
                    </td>
                    <td style={{ 
                      padding: '0.75rem 1rem', 
                      textAlign: 'center',
                      verticalAlign: 'middle'
                    }}>
                      <div style={{ 
                        display: 'flex', 
                        gap: '0.5rem', 
                        justifyContent: 'center', 
                        alignItems: 'center',
                        flexDirection: 'column'
                      }}>
                        <button
                          onClick={() => handleOpen(file)}
                          style={{
                            padding: '0.625rem 1.25rem',
                            borderRadius: '8px',
                            border: 'none',
                            background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
                            color: '#fff',
                            cursor: 'pointer',
                            fontSize: '0.875rem',
                            fontWeight: '600',
                            width: '100%',
                            maxWidth: '120px',
                            transition: 'all 0.2s cubic-bezier(0.4, 0, 0.2, 1)',
                            boxShadow: '0 2px 4px rgba(102, 126, 234, 0.3)'
                          }}
                          onMouseEnter={(e) => {
                            e.currentTarget.style.transform = 'translateY(-2px)';
                            e.currentTarget.style.boxShadow = '0 4px 8px rgba(102, 126, 234, 0.4)';
                          }}
                          onMouseLeave={(e) => {
                            e.currentTarget.style.transform = 'translateY(0)';
                            e.currentTarget.style.boxShadow = '0 2px 4px rgba(102, 126, 234, 0.3)';
                          }}
                          title="Open file"
                        >
                          Open
                        </button>
                        <button
                          onClick={() => handleDelete(file.id, fileName)}
                          disabled={deleteMutation.isPending}
                          style={{
                            padding: '0.625rem 1.25rem',
                            borderRadius: '8px',
                            border: 'none',
                            background: deleteMutation.isPending ? '#cbd5e1' : 'linear-gradient(135deg, #ef4444 0%, #dc2626 100%)',
                            color: '#fff',
                            cursor: deleteMutation.isPending ? 'not-allowed' : 'pointer',
                            fontSize: '0.875rem',
                            fontWeight: '600',
                            width: '100%',
                            maxWidth: '120px',
                            opacity: deleteMutation.isPending ? 0.7 : 1,
                            transition: 'all 0.2s cubic-bezier(0.4, 0, 0.2, 1)',
                            boxShadow: deleteMutation.isPending ? 'none' : '0 2px 4px rgba(239, 68, 68, 0.3)'
                          }}
                          onMouseEnter={(e) => {
                            if (!deleteMutation.isPending) {
                              e.currentTarget.style.transform = 'translateY(-2px)';
                              e.currentTarget.style.boxShadow = '0 4px 8px rgba(239, 68, 68, 0.4)';
                            }
                          }}
                          onMouseLeave={(e) => {
                            if (!deleteMutation.isPending) {
                              e.currentTarget.style.transform = 'translateY(0)';
                              e.currentTarget.style.boxShadow = '0 2px 4px rgba(239, 68, 68, 0.3)';
                            }
                          }}
                          title="Delete file"
                        >
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
    </div>
  );
});

FileList.displayName = 'FileList';

export default FileList;

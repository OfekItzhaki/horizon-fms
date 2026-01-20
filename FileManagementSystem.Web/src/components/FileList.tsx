import { useMutation, useQueryClient } from '@tanstack/react-query';
import { fileApi } from '../services/api';
import type { FileItemDto } from '../types';

interface FileListProps {
  files: FileItemDto[];
  isLoading: boolean;
  totalCount: number;
}

const FileList = ({ files, isLoading, totalCount }: FileListProps) => {
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
      <h2>Files ({totalCount})</h2>
      {files.length === 0 ? (
        <p>No files found</p>
      ) : (
        <div style={{ overflowX: 'auto', marginTop: '1rem' }}>
          <table style={{ 
            width: '100%', 
            borderCollapse: 'collapse', 
            border: '1px solid #ddd',
            tableLayout: 'fixed',
            backgroundColor: '#fff',
            boxShadow: '0 2px 4px rgba(0,0,0,0.1)'
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
                borderBottom: '2px solid #ccc', 
                background: 'linear-gradient(to bottom, #f8f9fa, #e9ecef)'
              }}>
                <th style={{ 
                  textAlign: 'left', 
                  padding: '1rem 0.75rem', 
                  borderRight: '1px solid #ddd',
                  fontWeight: '600',
                  fontSize: '0.9rem',
                  color: '#333'
                }}>Name</th>
                <th style={{ 
                  textAlign: 'right', 
                  padding: '1rem 0.75rem', 
                  borderRight: '1px solid #ddd',
                  fontWeight: '600',
                  fontSize: '0.9rem',
                  color: '#333'
                }}>Size</th>
                <th style={{ 
                  textAlign: 'left', 
                  padding: '1rem 0.75rem', 
                  borderRight: '1px solid #ddd',
                  fontWeight: '600',
                  fontSize: '0.9rem',
                  color: '#333'
                }}>Type</th>
                <th style={{ 
                  textAlign: 'left', 
                  padding: '1rem 0.75rem', 
                  borderRight: '1px solid #ddd',
                  fontWeight: '600',
                  fontSize: '0.9rem',
                  color: '#333'
                }}>Tags</th>
                <th style={{ 
                  textAlign: 'left', 
                  padding: '1rem 0.75rem', 
                  borderRight: '1px solid #ddd',
                  fontWeight: '600',
                  fontSize: '0.9rem',
                  color: '#333'
                }}>Date</th>
                <th style={{ 
                  textAlign: 'center', 
                  padding: '1rem 0.75rem',
                  fontWeight: '600',
                  fontSize: '0.9rem',
                  color: '#333'
                }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {files.map((file, index) => {
                // Use FileName if available, otherwise extract from path
                const fileName = file.fileName || getFileName(file.path);
                
                return (
                  <tr 
                    key={file.id} 
                    style={{ 
                      borderBottom: '1px solid #e0e0e0',
                      backgroundColor: index % 2 === 0 ? '#fff' : '#f9f9f9',
                      transition: 'background-color 0.2s'
                    }}
                    onMouseEnter={(e) => {
                      e.currentTarget.style.backgroundColor = '#f0f7ff';
                    }}
                    onMouseLeave={(e) => {
                      e.currentTarget.style.backgroundColor = index % 2 === 0 ? '#fff' : '#f9f9f9';
                    }}
                  >
                    <td style={{ 
                      padding: '0.875rem 0.75rem', 
                      borderRight: '1px solid #e0e0e0',
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                      whiteSpace: 'nowrap',
                      fontSize: '0.9rem',
                      maxWidth: 0
                    }} title={fileName}>
                      {fileName}
                    </td>
                    <td style={{ 
                      textAlign: 'right', 
                      padding: '0.875rem 0.75rem', 
                      borderRight: '1px solid #e0e0e0',
                      whiteSpace: 'nowrap',
                      fontSize: '0.9rem',
                      color: '#666'
                    }}>
                      {formatSize(file.size)}
                    </td>
                    <td style={{ 
                      padding: '0.875rem 0.75rem', 
                      borderRight: '1px solid #e0e0e0',
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                      whiteSpace: 'nowrap',
                      fontSize: '0.85rem',
                      color: '#666'
                    }} title={file.mimeType}>
                      {file.mimeType || '-'}
                    </td>
                    <td style={{ 
                      padding: '0.875rem 0.75rem', 
                      borderRight: '1px solid #e0e0e0',
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                      whiteSpace: 'nowrap',
                      fontSize: '0.85rem',
                      color: '#666'
                    }} title={file.tags.join(', ') || ''}>
                      {file.tags.length > 0 ? file.tags.join(', ') : '-'}
                    </td>
                    <td style={{ 
                      padding: '0.875rem 0.75rem', 
                      borderRight: '1px solid #e0e0e0',
                      whiteSpace: 'nowrap',
                      fontSize: '0.9rem',
                      color: '#666'
                    }}>
                      {new Date(file.createdDate).toLocaleDateString()}
                    </td>
                    <td style={{ 
                      padding: '0.875rem', 
                      textAlign: 'center',
                      borderRight: 'none'
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
                            padding: '0.5rem 1rem',
                            borderRadius: '6px',
                            border: 'none',
                            background: '#007bff',
                            color: '#fff',
                            cursor: 'pointer',
                            fontSize: '0.875rem',
                            fontWeight: '500',
                            width: '100%',
                            maxWidth: '120px',
                            transition: 'all 0.2s',
                            boxShadow: '0 1px 3px rgba(0,123,255,0.3)'
                          }}
                          onMouseEnter={(e) => {
                            e.currentTarget.style.background = '#0056b3';
                            e.currentTarget.style.transform = 'translateY(-1px)';
                            e.currentTarget.style.boxShadow = '0 2px 5px rgba(0,123,255,0.4)';
                          }}
                          onMouseLeave={(e) => {
                            e.currentTarget.style.background = '#007bff';
                            e.currentTarget.style.transform = 'translateY(0)';
                            e.currentTarget.style.boxShadow = '0 1px 3px rgba(0,123,255,0.3)';
                          }}
                          title="Open file"
                        >
                          Open
                        </button>
                        <button
                          onClick={() => handleDelete(file.id, fileName)}
                          disabled={deleteMutation.isPending}
                          style={{
                            padding: '0.5rem 1rem',
                            borderRadius: '6px',
                            border: 'none',
                            background: '#dc3545',
                            color: '#fff',
                            cursor: deleteMutation.isPending ? 'not-allowed' : 'pointer',
                            fontSize: '0.875rem',
                            fontWeight: '500',
                            width: '100%',
                            maxWidth: '120px',
                            opacity: deleteMutation.isPending ? 0.6 : 1,
                            transition: 'all 0.2s',
                            boxShadow: '0 1px 3px rgba(220,53,69,0.3)'
                          }}
                          onMouseEnter={(e) => {
                            if (!deleteMutation.isPending) {
                              e.currentTarget.style.background = '#c82333';
                              e.currentTarget.style.transform = 'translateY(-1px)';
                              e.currentTarget.style.boxShadow = '0 2px 5px rgba(220,53,69,0.4)';
                            }
                          }}
                          onMouseLeave={(e) => {
                            if (!deleteMutation.isPending) {
                              e.currentTarget.style.background = '#dc3545';
                              e.currentTarget.style.transform = 'translateY(0)';
                              e.currentTarget.style.boxShadow = '0 1px 3px rgba(220,53,69,0.3)';
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
      )}
    </div>
  );
};

export default FileList;

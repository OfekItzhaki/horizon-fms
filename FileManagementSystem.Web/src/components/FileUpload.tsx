import { useCallback, useState, memo } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { fileApi } from '../services/api';

interface FileUploadProps {
  destinationFolderId?: string;
}

const FileUpload = memo(({ destinationFolderId }: FileUploadProps) => {
  const [isDragging, setIsDragging] = useState(false);
  const [uploadProgress, setUploadProgress] = useState<{ [key: string]: number }>({});
  const queryClient = useQueryClient();

  const uploadMutation = useMutation({
    mutationFn: async (file: File) => {
      return await fileApi.uploadFile(file, destinationFolderId);
    },
    onSuccess: () => {
      // Invalidate and refetch files
      queryClient.invalidateQueries({ queryKey: ['files'] });
      queryClient.invalidateQueries({ queryKey: ['folders'] });
    },
  });

  const handleDragEnter = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(true);
  }, []);

  const handleDragLeave = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setIsDragging(false);
  }, []);

  const handleDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
  }, []);

  const handleDrop = useCallback(
    async (e: React.DragEvent) => {
      e.preventDefault();
      e.stopPropagation();
      setIsDragging(false);

      const files = Array.from(e.dataTransfer.files);
      if (files.length === 0) return;

      // Upload all files
      for (const file of files) {
        try {
          await uploadMutation.mutateAsync(file);
        } catch (error) {
          console.error(`Error uploading ${file.name}:`, error);
          alert(`Failed to upload ${file.name}`);
        }
      }
    },
    [uploadMutation]
  );

  const handleFileInput = useCallback(
    async (e: React.ChangeEvent<HTMLInputElement>) => {
      const files = Array.from(e.target.files || []);
      if (files.length === 0) return;

      // Upload all files
      for (const file of files) {
        try {
          await uploadMutation.mutateAsync(file);
        } catch (error) {
          console.error(`Error uploading ${file.name}:`, error);
          alert(`Failed to upload ${file.name}`);
        }
      }

      // Reset input
      e.target.value = '';
    },
    [uploadMutation]
  );

  return (
    <div
      onDragEnter={handleDragEnter}
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
      style={{
        border: `2px dashed ${isDragging ? '#667eea' : '#cbd5e1'}`,
        borderRadius: '12px',
        padding: '3rem 2rem',
        textAlign: 'center',
        background: isDragging 
          ? 'linear-gradient(135deg, rgba(102, 126, 234, 0.1) 0%, rgba(118, 75, 162, 0.1) 100%)' 
          : '#ffffff',
        cursor: 'pointer',
        transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
        marginBottom: '1.5rem',
        boxShadow: isDragging 
          ? '0 10px 25px -5px rgba(102, 126, 234, 0.2), 0 8px 10px -6px rgba(102, 126, 234, 0.1)'
          : '0 1px 3px 0 rgba(0, 0, 0, 0.1), 0 1px 2px 0 rgba(0, 0, 0, 0.06)',
        transform: isDragging ? 'scale(1.02)' : 'scale(1)',
      }}
    >
      <input
        type="file"
        multiple
        onChange={handleFileInput}
        style={{ display: 'none' }}
        id="file-upload-input"
      />
      <label
        htmlFor="file-upload-input"
        style={{
          cursor: 'pointer',
          display: 'block',
          width: '100%',
          height: '100%',
        }}
      >
        {isDragging ? (
          <div>
            <div style={{ fontSize: '3rem', marginBottom: '1rem' }}>📤</div>
            <p style={{ 
              fontSize: '1.25rem', 
              fontWeight: '600', 
              color: '#667eea',
              margin: 0
            }}>
              Drop files here to upload
            </p>
          </div>
        ) : (
          <div>
            <div style={{ fontSize: '2.5rem', marginBottom: '0.75rem' }}>📁</div>
            <p style={{ 
              fontSize: '1rem', 
              color: '#475569',
              fontWeight: '500',
              margin: '0 0 0.5rem 0'
            }}>
              Drag and drop files here, or click to select files
            </p>
            <p style={{ 
              fontSize: '0.875rem', 
              color: '#94a3b8', 
              margin: 0
            }}>
              Multiple files supported
            </p>
          </div>
        )}
      </label>
      {uploadMutation.isPending && (
        <div style={{ 
          marginTop: '1.5rem', 
          color: '#667eea',
          fontSize: '0.95rem',
          fontWeight: '500',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          gap: '0.5rem'
        }}>
          <span style={{ 
            display: 'inline-block',
            width: '16px',
            height: '16px',
            border: '2px solid #667eea',
            borderTopColor: 'transparent',
            borderRadius: '50%',
            animation: 'spin 0.8s linear infinite'
          }}></span>
          Uploading files...
        </div>
      )}
    </div>
  );
});

FileUpload.displayName = 'FileUpload';

export default FileUpload;

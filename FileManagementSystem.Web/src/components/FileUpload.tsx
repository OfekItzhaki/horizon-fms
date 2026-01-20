import { useCallback, useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { fileApi } from '../services/api';

interface FileUploadProps {
  destinationFolderId?: string;
}

const FileUpload = ({ destinationFolderId }: FileUploadProps) => {
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
        border: `2px dashed ${isDragging ? '#007bff' : '#ccc'}`,
        borderRadius: '8px',
        padding: '2rem',
        textAlign: 'center',
        background: isDragging ? '#f0f8ff' : '#fafafa',
        cursor: 'pointer',
        transition: 'all 0.2s',
        marginBottom: '1rem',
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
            <p style={{ fontSize: '1.2rem', fontWeight: 'bold', color: '#007bff' }}>
              Drop files here to upload
            </p>
          </div>
        ) : (
          <div>
            <p style={{ fontSize: '1rem', color: '#666' }}>
              Drag and drop files here, or click to select files
            </p>
            <p style={{ fontSize: '0.9rem', color: '#999', marginTop: '0.5rem' }}>
              Multiple files supported
            </p>
          </div>
        )}
      </label>
      {uploadMutation.isPending && (
        <div style={{ marginTop: '1rem', color: '#007bff' }}>
          Uploading files...
        </div>
      )}
    </div>
  );
};

export default FileUpload;

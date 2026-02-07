import { useCallback, useState, memo } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'react-hot-toast';
import { fileApi } from '../services/api';
import './FileUpload.css';

interface FileUploadProps {
  destinationFolderId?: string;
}

const FileUpload = memo(({ destinationFolderId }: FileUploadProps) => {
  const [isDragging, setIsDragging] = useState(false);
  const queryClient = useQueryClient();

  const uploadMutation = useMutation({
    mutationFn: async (file: File) => {
      return await fileApi.uploadFile(file, destinationFolderId);
    },
    onSuccess: (_, file) => {
      queryClient.invalidateQueries({ queryKey: ['files'] });
      queryClient.invalidateQueries({ queryKey: ['folders'] });
      toast.success(`Successfully uploaded ${file.name}`);
    },
    onError: (error: Error, file) => {
      toast.error(`Failed to upload ${file.name}: ${error.message || 'Unknown error'}`);
    }
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

      for (const file of files) {
        uploadMutation.mutate(file);
      }
    },
    [uploadMutation]
  );

  const handleFileInput = useCallback(
    async (e: React.ChangeEvent<HTMLInputElement>) => {
      const files = Array.from(e.target.files || []);
      if (files.length === 0) return;

      for (const file of files) {
        uploadMutation.mutate(file);
      }
      e.target.value = '';
    },
    [uploadMutation]
  );

  return (
    <div
      className={`upload-container ${isDragging ? 'dragging' : ''}`}
      onDragEnter={handleDragEnter}
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
    >
      <input
        type="file"
        multiple
        onChange={handleFileInput}
        style={{ display: 'none' }}
        id="file-upload-input"
      />
      <label htmlFor="file-upload-input" className="upload-label">
        {isDragging ? (
          <div>
            <div className="upload-icon large">📤</div>
            <p className="upload-text highlight">Drop files here to upload</p>
          </div>
        ) : (
          <div>
            <div className="upload-icon">📁</div>
            <p className="upload-text">Drag and drop files here, or click to select files</p>
            <p className="upload-subtext">Multiple files supported</p>
          </div>
        )}
      </label>
      {uploadMutation.isPending && (
        <div className="uploading-status">
          <span className="spinner"></span>
          Uploading files...
        </div>
      )}
    </div>
  );
});

FileUpload.displayName = 'FileUpload';

export default FileUpload;

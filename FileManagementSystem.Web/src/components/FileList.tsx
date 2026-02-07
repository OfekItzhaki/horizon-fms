import { useMutation, useQueryClient } from '@tanstack/react-query';
import { memo, useCallback } from 'react';
import { toast } from 'react-hot-toast';
import { fileApi } from '../services/api';
import type { FileItemDto } from '../types';
import './FileList.css';

interface FileListProps {
  files: FileItemDto[];
  isLoading: boolean;
  totalCount: number;
}

const formatSize = (bytes: number) => {
  if (bytes < 1024) return bytes + ' B';
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB';
  return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
};

const getFileName = (path: string) => {
  if (!path) return '';
  let normalized = path.replace(/^[A-Za-z]:[\\\/]?/i, '');
  normalized = normalized.replace(/\\/g, '/');
  normalized = normalized.replace(/^\/+/, '');
  return normalized.split('/').pop() || path;
};

const FileTableRow = memo(({
  file,
  index,
  onDelete,
  onOpen,
  isDeleting
}: {
  file: FileItemDto;
  index: number;
  onDelete: (id: string, name: string) => void;
  onOpen: (file: FileItemDto) => void;
  isDeleting: boolean;
}) => {
  const fileName = file.fileName || getFileName(file.path ?? '');

  return (
    <tr className={`file-row ${index % 2 === 0 ? 'even' : 'odd'}`}>
      <td className="file-cell" title={fileName}>{fileName}</td>
      <td className="file-cell secondary align-right">{formatSize(file.size ?? 0)}</td>
      <td className="file-cell secondary" title={file.mimeType ?? undefined}>{file.mimeType || '-'}</td>
      <td className="file-cell secondary" title={file.tags?.join(', ') || ''}>
        {file.tags && file.tags.length > 0 ? file.tags.join(', ') : '-'}
      </td>
      <td className="file-cell secondary">
        {file.createdDate ? new Date(file.createdDate).toLocaleDateString() : '-'}
      </td>
      <td className="file-cell">
        <div className="action-buttons">
          <button className="btn-open" onClick={() => onOpen(file)}>Open</button>
          <button
            className="btn-delete"
            onClick={() => onDelete(file.id!, fileName)}
            disabled={isDeleting}
          >
            {isDeleting ? '...' : 'Delete'}
          </button>
        </div>
      </td>
    </tr>
  );
});

const FileList = memo(({ files, isLoading, totalCount }: FileListProps) => {
  const queryClient = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await fileApi.deleteFile(id, true);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['files'] });
      toast.success('File deleted successfully');
    },
    onError: (error: Error) => {
      toast.error(`Failed to delete file: ${error.message || 'Unknown error'}`);
    }
  });

  const handleDelete = useCallback(async (id: string, fileName: string) => {
    if (window.confirm(`Are you sure you want to delete "${fileName}"?`)) {
      deleteMutation.mutate(id);
    }
  }, [deleteMutation]);

  const handleOpen = useCallback(async (file: FileItemDto) => {
    try {
      if (!file.id) return;
      const downloadUrl = await fileApi.downloadFile(file.id);
      window.open(downloadUrl, '_blank');
    } catch (error) {
      toast.error(`Unable to open file: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  }, []);

  if (isLoading) {
    return <div className="loading-container">Loading files...</div>;
  }

  return (
    <div>
      <h2 className="file-list-title">
        Files <span className="file-list-count">({totalCount})</span>
      </h2>
      <div className="file-table-container">
        <table className="file-table">
          <colgroup>
            <col style={{ width: '30%' }} />
            <col style={{ width: '10%' }} />
            <col style={{ width: '15%' }} />
            <col style={{ width: '15%' }} />
            <col style={{ width: '15%' }} />
            <col style={{ width: '15%' }} />
          </colgroup>
          <thead>
            <tr>
              <th>Name</th>
              <th className="align-right">Size</th>
              <th>Type</th>
              <th>Tags</th>
              <th>Date</th>
              <th className="align-center">Actions</th>
            </tr>
          </thead>
          <tbody>
            {files.length === 0 ? (
              <tr>
                <td colSpan={6} style={{ padding: '3rem', textAlign: 'center', color: '#94a3b8' }}>
                  No files found
                </td>
              </tr>
            ) : (
              files.map((file, index) => (
                <FileTableRow
                  key={file.id}
                  file={file}
                  index={index}
                  onDelete={handleDelete}
                  onOpen={handleOpen}
                  isDeleting={deleteMutation.isPending && deleteMutation.variables === file.id}
                />
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
});

FileList.displayName = 'FileList';
FileTableRow.displayName = 'FileTableRow';

export default FileList;

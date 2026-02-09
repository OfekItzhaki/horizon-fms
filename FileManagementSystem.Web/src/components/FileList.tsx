import { useMutation, useQueryClient } from '@tanstack/react-query';
import { memo, useCallback, useState, useRef, useEffect } from 'react';
import { toast } from 'react-hot-toast';
import { Edit2, ExternalLink, Trash2, MoreVertical, Eye } from 'lucide-react';
import { fileApi } from '../services/api';
import type { FileItemDto } from '../types';
import TagEditor from './TagEditor';
import { LoadingSpinner } from './LoadingSpinner';
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
  // Normalize path separators to forward slashes
  // eslint-disable-next-line no-useless-escape
  let normalized = path.replace(/^[A-Za-z]:[\\\/]?/i, '');
  normalized = normalized.replace(/\\/g, '/');
  normalized = normalized.replace(/^\/+/, '');
  return normalized.split('/').pop() || path;
};

const FileTableRow = memo(
  ({
    file,
    index,
    onDelete,
    onOpen,
    onEditTags,
    isDeleting,
  }: {
    file: FileItemDto;
    index: number;
    onDelete: (id: string, name: string) => void;
    onOpen: (file: FileItemDto) => void;
    onEditTags: (file: FileItemDto) => void;
    isDeleting: boolean;
  }) => {
    const fileName = file.fileName || getFileName(file.path ?? '');
    const [isDropdownOpen, setIsDropdownOpen] = useState(false);
    const dropdownRef = useRef<HTMLDivElement>(null);

    // Close dropdown when clicking outside
    useEffect(() => {
      const handleClickOutside = (event: MouseEvent) => {
        if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
          setIsDropdownOpen(false);
        }
      };

      if (isDropdownOpen) {
        document.addEventListener('mousedown', handleClickOutside);
      }

      return () => {
        document.removeEventListener('mousedown', handleClickOutside);
      };
    }, [isDropdownOpen]);

    const handleRename = () => {
      setIsDropdownOpen(false);
      const newName = prompt('Enter new name:', fileName);
      if (newName && newName !== fileName) {
        // TODO: Implement rename functionality
        toast.error('Rename functionality not yet implemented');
      }
    };

    const handleView = () => {
      setIsDropdownOpen(false);
      onOpen(file);
    };

    const handleDeleteClick = () => {
      setIsDropdownOpen(false);
      onDelete(file.id!, fileName);
    };

    return (
      <tr className={`file-row ${index % 2 === 0 ? 'even' : 'odd'}`}>
        <td className='file-cell' title={fileName}>
          <div className='flex items-center gap-2'>{fileName}</div>
        </td>
        <td className='file-cell secondary align-right'>{formatSize(file.size ?? 0)}</td>
        <td className='file-cell secondary' title={file.mimeType ?? undefined}>
          {file.mimeType || '-'}
        </td>
        <td className='file-cell secondary'>
          <div className='flex items-center gap-2 group'>
            <div className='flex flex-wrap gap-1 max-w-[200px]'>
              {file.tags && file.tags.length > 0 ? (
                file.tags.map((tag) => (
                  <span
                    key={tag}
                    className='inline-block px-1.5 py-0.5 rounded text-xs bg-[var(--bg-secondary)] border border-[var(--border-color)] text-[var(--text-secondary)]'
                  >
                    {tag}
                  </span>
                ))
              ) : (
                <span className='text-[var(--text-tertiary)] italic text-xs'>No tags</span>
              )}
            </div>
            <button
              onClick={() => onEditTags(file)}
              className='opacity-0 group-hover:opacity-100 p-1 rounded hover:bg-[var(--surface-secondary)] text-[var(--text-secondary)] transition-all'
              title='Edit tags'
            >
              <Edit2 size={14} />
            </button>
          </div>
        </td>
        <td className='file-cell secondary'>
          {file.createdDate ? new Date(file.createdDate).toLocaleDateString() : '-'}
        </td>
        <td className='file-cell'>
          <div className='action-buttons flex gap-2 justify-end items-center'>
            <button
              className='p-1.5 rounded hover:bg-[var(--surface-secondary)] text-[var(--accent-primary)] transition-colors'
              onClick={() => onOpen(file)}
              title='Download'
            >
              <ExternalLink size={16} />
            </button>
            <div className='relative' ref={dropdownRef}>
              <button
                className='p-1.5 rounded hover:bg-[var(--surface-secondary)] text-[var(--text-secondary)] transition-colors'
                onClick={() => setIsDropdownOpen(!isDropdownOpen)}
                title='More actions'
              >
                <MoreVertical size={16} />
              </button>
              {isDropdownOpen && (
                <div className='absolute right-0 mt-1 w-40 bg-[var(--surface-primary)] border border-[var(--border-color)] rounded-lg shadow-lg z-10 overflow-hidden'>
                  <button
                    className='w-full px-4 py-2 text-left text-sm hover:bg-[var(--surface-secondary)] text-[var(--text-primary)] flex items-center gap-2 transition-colors'
                    onClick={handleView}
                  >
                    <Eye size={14} />
                    View
                  </button>
                  <button
                    className='w-full px-4 py-2 text-left text-sm hover:bg-[var(--surface-secondary)] text-[var(--text-primary)] flex items-center gap-2 transition-colors'
                    onClick={handleRename}
                  >
                    <Edit2 size={14} />
                    Rename
                  </button>
                  <button
                    className='w-full px-4 py-2 text-left text-sm hover:bg-red-50 text-red-500 flex items-center gap-2 transition-colors disabled:opacity-50'
                    onClick={handleDeleteClick}
                    disabled={isDeleting}
                  >
                    <Trash2 size={14} />
                    Delete
                  </button>
                </div>
              )}
            </div>
          </div>
        </td>
      </tr>
    );
  },
);

const FileList = memo(({ files, isLoading, totalCount }: FileListProps) => {
  const queryClient = useQueryClient();
  const [editingFile, setEditingFile] = useState<FileItemDto | null>(null);

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
    },
  });

  const handleDelete = useCallback(
    async (id: string, fileName: string) => {
      if (window.confirm(`Are you sure you want to delete "${fileName}"?`)) {
        deleteMutation.mutate(id);
      }
    },
    [deleteMutation],
  );

  const handleOpen = useCallback(async (file: FileItemDto) => {
    try {
      if (!file.id) return;
      const downloadUrl = await fileApi.downloadFile(file.id);
      window.open(downloadUrl, '_blank');
    } catch (error) {
      toast.error(
        `Unable to open file: ${error instanceof Error ? error.message : 'Unknown error'}`,
      );
    }
  }, []);

  const handleEditTags = useCallback((file: FileItemDto) => {
    setEditingFile(file);
  }, []);

  const handleTagsUpdated = () => {
    queryClient.invalidateQueries({ queryKey: ['files'] });
  };

  if (isLoading) {
    return (
      <div className='loading-container flex flex-col items-center justify-center py-12 text-[var(--text-tertiary)]'>
        <LoadingSpinner size='lg' color='var(--accent-primary)' />
        <span className='mt-4'>Loading files...</span>
      </div>
    );
  }

  return (
    <div>
      <h2 className='file-list-title text-xl font-semibold mb-4 text-[var(--text-primary)]'>
        Files{' '}
        <span className='text-[var(--text-tertiary)] text-base font-normal'>({totalCount})</span>
      </h2>
      <div className='file-table-container bg-[var(--surface-primary)] rounded-xl border border-[var(--border-color)] overflow-hidden shadow-sm'>
        <table className='file-table w-full'>
          <colgroup>
            <col style={{ width: '30%' }} />
            <col style={{ width: '10%' }} />
            <col style={{ width: '15%' }} />
            <col style={{ width: '20%' }} />
            <col style={{ width: '15%' }} />
            <col style={{ width: '10%' }} />
          </colgroup>
          <thead className='bg-[var(--bg-secondary)] border-b border-[var(--border-color)]'>
            <tr>
              <th className='text-left py-3 px-4 font-medium text-[var(--text-secondary)]'>Name</th>
              <th className='text-right py-3 px-4 font-medium text-[var(--text-secondary)]'>
                Size
              </th>
              <th className='text-left py-3 px-4 font-medium text-[var(--text-secondary)]'>Type</th>
              <th className='text-left py-3 px-4 font-medium text-[var(--text-secondary)]'>Tags</th>
              <th className='text-left py-3 px-4 font-medium text-[var(--text-secondary)]'>Date</th>
              <th className='text-right py-3 px-4 font-medium text-[var(--text-secondary)]'>
                Actions
              </th>
            </tr>
          </thead>
          <tbody className='divide-y divide-[var(--border-color)]'>
            {files.length === 0 ? (
              <tr>
                <td
                  colSpan={6}
                  style={{ padding: '3rem', textAlign: 'center', color: 'var(--text-tertiary)' }}
                >
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
                  onEditTags={handleEditTags}
                  isDeleting={deleteMutation.isPending && deleteMutation.variables === file.id}
                />
              ))
            )}
          </tbody>
        </table>
      </div>

      {editingFile && (
        <TagEditor
          fileId={editingFile.id!}
          initialTags={editingFile.tags || []}
          isOpen={true}
          onClose={() => setEditingFile(null)}
          onTagsUpdated={handleTagsUpdated}
        />
      )}
    </div>
  );
});

FileList.displayName = 'FileList';
FileTableRow.displayName = 'FileTableRow';

export default FileList;

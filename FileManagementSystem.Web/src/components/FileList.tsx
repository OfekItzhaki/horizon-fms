import type { FileItemDto } from '../types';

interface FileListProps {
  files: FileItemDto[];
  isLoading: boolean;
  totalCount: number;
}

const FileList = ({ files, isLoading, totalCount }: FileListProps) => {
  const formatSize = (bytes: number) => {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(2) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
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
        <table style={{ width: '100%', borderCollapse: 'collapse', border: '1px solid #ddd' }}>
          <thead>
            <tr style={{ borderBottom: '2px solid #ccc', background: '#f5f5f5' }}>
              <th style={{ textAlign: 'left', padding: '0.75rem', borderRight: '1px solid #ddd' }}>Name</th>
              <th style={{ textAlign: 'right', padding: '0.75rem', borderRight: '1px solid #ddd' }}>Size</th>
              <th style={{ textAlign: 'left', padding: '0.75rem', borderRight: '1px solid #ddd' }}>Type</th>
              <th style={{ textAlign: 'left', padding: '0.75rem', borderRight: '1px solid #ddd' }}>Tags</th>
              <th style={{ textAlign: 'left', padding: '0.75rem' }}>Date</th>
            </tr>
          </thead>
          <tbody>
            {files.map((file) => {
              // Extract just the filename from the path, removing drive letters and long paths
              const getFileName = (path: string) => {
                const normalized = path.replace(/^[A-Z]:\\?/, '').replace(/\\/g, '/');
                return normalized.split('/').pop() || path;
              };
              
              return (
                <tr key={file.id} style={{ borderBottom: '1px solid #ddd' }}>
                  <td style={{ padding: '0.75rem', borderRight: '1px solid #ddd' }}>{getFileName(file.path)}</td>
                  <td style={{ textAlign: 'right', padding: '0.75rem', borderRight: '1px solid #ddd' }}>{formatSize(file.size)}</td>
                  <td style={{ padding: '0.75rem', borderRight: '1px solid #ddd' }}>{file.mimeType}</td>
                  <td style={{ padding: '0.75rem', borderRight: '1px solid #ddd' }}>{file.tags.join(', ') || '-'}</td>
                  <td style={{ padding: '0.75rem' }}>{new Date(file.createdDate).toLocaleDateString()}</td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </div>
  );
};

export default FileList;

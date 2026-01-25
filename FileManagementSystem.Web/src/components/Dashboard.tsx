import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { fileApi, folderApi } from '../services/api';
import FileList from './FileList';
import FolderTree from './FolderTree';
import SearchBar from './SearchBar';
import FileUpload from './FileUpload';

const Dashboard = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedFolderId, setSelectedFolderId] = useState<string | undefined>();
  const [isPhotoOnly, setIsPhotoOnly] = useState(false);
  const [isDocumentOnly, setIsDocumentOnly] = useState(false);

  const { data: filesData, isLoading: filesLoading } = useQuery({
    queryKey: ['files', searchTerm, selectedFolderId, isPhotoOnly],
    queryFn: () => fileApi.getFiles({
      searchTerm: searchTerm || undefined,
      folderId: selectedFolderId,
      isPhoto: isPhotoOnly || undefined,
    }),
  });

  // Filter documents on frontend (common document mime types)
  const documentMimeTypes = [
    'application/pdf',
    'application/msword',
    'application/vnd.openxmlformats-officedocument',
    'application/vnd.ms-excel',
    'application/vnd.ms-powerpoint',
    'text/',
    'application/rtf',
    'application/vnd.oasis.opendocument',
  ];

  const filteredFiles = filesData?.files.filter(file => {
    if (isDocumentOnly) {
      return documentMimeTypes.some(mimeType => 
        file.mimeType.toLowerCase().startsWith(mimeType.toLowerCase())
      );
    }
    return true;
  }) || [];

  const { data: foldersData } = useQuery({
    queryKey: ['folders'],
    queryFn: folderApi.getFolders,
  });

  return (
    <div style={{ 
      display: 'flex', 
      height: '100vh', 
      flexDirection: 'column',
      backgroundColor: '#f8fafc',
      fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
    }}>
      <header style={{ 
        padding: '1.5rem 2rem', 
        borderBottom: '1px solid #e2e8f0', 
        background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
        boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1), 0 2px 4px -1px rgba(0, 0, 0, 0.06)'
      }}>
        <h1 style={{ 
          margin: '0 0 1rem 0', 
          fontSize: '1.75rem', 
          fontWeight: '700',
          color: '#ffffff',
          letterSpacing: '-0.025em'
        }}>
          File Management System
        </h1>
        <SearchBar
          searchTerm={searchTerm}
          onSearchChange={setSearchTerm}
          isPhotoOnly={isPhotoOnly}
          onPhotoOnlyChange={setIsPhotoOnly}
          isDocumentOnly={isDocumentOnly}
          onDocumentOnlyChange={setIsDocumentOnly}
        />
      </header>
      
      <div style={{ display: 'flex', flex: 1, overflow: 'hidden' }}>
        <aside style={{ 
          width: '320px', 
          borderRight: '1px solid #e2e8f0', 
          overflow: 'auto', 
          padding: '1.5rem',
          backgroundColor: '#ffffff',
          boxShadow: '2px 0 8px rgba(0, 0, 0, 0.04)'
        }}>
          <FolderTree
            folders={foldersData?.folders || []}
            onFolderSelect={setSelectedFolderId}
            selectedFolderId={selectedFolderId}
          />
        </aside>
        
        <main style={{ 
          flex: 1, 
          overflow: 'auto', 
          padding: '2rem',
          backgroundColor: '#f8fafc'
        }}>
          <FileUpload destinationFolderId={selectedFolderId} />
          <FileList
            files={filteredFiles}
            isLoading={filesLoading}
            totalCount={isDocumentOnly ? filteredFiles.length : (filesData?.totalCount || 0)}
          />
        </main>
      </div>
    </div>
  );
};

export default Dashboard;

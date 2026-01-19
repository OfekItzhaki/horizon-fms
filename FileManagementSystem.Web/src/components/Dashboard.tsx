import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { fileApi, folderApi } from '../services/api';
import FileList from './FileList';
import FolderTree from './FolderTree';
import SearchBar from './SearchBar';

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
    <div style={{ display: 'flex', height: '100vh', flexDirection: 'column' }}>
      <header style={{ padding: '1rem', borderBottom: '1px solid #ccc', background: '#f5f5f5' }}>
        <h1>File Management System</h1>
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
        <aside style={{ width: '300px', borderRight: '1px solid #ccc', overflow: 'auto', padding: '1rem' }}>
          <FolderTree
            folders={foldersData?.folders || []}
            onFolderSelect={setSelectedFolderId}
            selectedFolderId={selectedFolderId}
          />
        </aside>
        
        <main style={{ flex: 1, overflow: 'auto', padding: '1rem' }}>
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

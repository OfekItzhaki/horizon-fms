import { useState, useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { fileApi, folderApi } from '../services/api';
import FileList from './FileList';
import FolderTree from './FolderTree';
import SearchBar from './SearchBar';
import FileUpload from './FileUpload';
import './Dashboard.css';

const Dashboard = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedFolderId, setSelectedFolderId] = useState<string | undefined>();
  const [isPhotoOnly, setIsPhotoOnly] = useState(false);
  const [isDocumentOnly, setIsDocumentOnly] = useState(false);
  const [isMobile, setIsMobile] = useState(window.innerWidth <= 768);

  useEffect(() => {
    const handleResize = () => {
      setIsMobile(window.innerWidth <= 768);
    };
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);

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
      <header className="dashboard-header">
        <div className="dashboard-header-overlay"></div>
        <div className="dashboard-header-content">
        <h1 style={{ 
          margin: '0 0 1rem 0', 
          fontSize: '2rem', 
          fontWeight: '800',
          color: '#ffffff',
          letterSpacing: '-0.03em',
          textShadow: '0 2px 8px rgba(0, 0, 0, 0.2), 0 1px 3px rgba(0, 0, 0, 0.3)',
          fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
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
        </div>
      </header>
      
      <div className="dashboard-layout">
        <aside className="dashboard-sidebar">
          <FolderTree
            folders={foldersData?.folders || []}
            onFolderSelect={setSelectedFolderId}
            selectedFolderId={selectedFolderId}
          />
        </aside>
        
        <main className="dashboard-main">
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

import { useState, useEffect, memo, useCallback } from 'react';
import { useQuery } from '@tanstack/react-query';
import { fileApi, folderApi } from '../services/api';
import type { FileItemDto } from '../services/api';
import FileList from './FileList';
import FolderTree from './FolderTree';
import SearchBar from './SearchBar';
import FileUpload from './FileUpload';
import ThemeToggle from './ThemeToggle';
import './Dashboard.css';

// Memoized header component to prevent re-renders when only search results change
// Only re-renders when filter props change, not when searchTerm changes
const DashboardHeader = memo(
  ({
    searchTerm,
    onSearchChange,
    isPhotoOnly,
    onPhotoOnlyChange,
    isDocumentOnly,
    onDocumentOnlyChange,
  }: {
    searchTerm: string;
    onSearchChange: (term: string) => void;
    isPhotoOnly: boolean;
    onPhotoOnlyChange: (value: boolean) => void;
    isDocumentOnly: boolean;
    onDocumentOnlyChange: (value: boolean) => void;
  }) => (
    <header className='dashboard-header'>
      <div className='dashboard-header-overlay'></div>
      <div className='dashboard-header-content'>
        <h1
          style={{
            margin: '0 0 1rem 0',
            fontSize: '2rem',
            fontWeight: '800',
            color: '#ffffff',
            letterSpacing: '-0.03em',
            textShadow: '0 2px 8px rgba(0, 0, 0, 0.2), 0 1px 3px rgba(0, 0, 0, 0.3)',
            fontFamily:
              '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif',
          }}
        >
          File Management System
        </h1>
        <div className='flex items-center gap-4 w-full'>
          <SearchBar
            searchTerm={searchTerm}
            onSearchChange={onSearchChange}
            isPhotoOnly={isPhotoOnly}
            onPhotoOnlyChange={onPhotoOnlyChange}
            isDocumentOnly={isDocumentOnly}
            onDocumentOnlyChange={onDocumentOnlyChange}
          />
          <ThemeToggle />
        </div>
      </div>
    </header>
  ),
  (prevProps, nextProps) => {
    // Custom comparison: return true if props are equal (don't re-render)
    // We ignore searchTerm changes - only re-render if filters or callbacks change
    const filtersEqual =
      prevProps.isPhotoOnly === nextProps.isPhotoOnly &&
      prevProps.isDocumentOnly === nextProps.isDocumentOnly;
    const callbacksEqual =
      prevProps.onSearchChange === nextProps.onSearchChange &&
      prevProps.onPhotoOnlyChange === nextProps.onPhotoOnlyChange &&
      prevProps.onDocumentOnlyChange === nextProps.onDocumentOnlyChange;
    // Return true (don't re-render) if filters and callbacks are equal, regardless of searchTerm
    return filtersEqual && callbacksEqual;
  },
);
DashboardHeader.displayName = 'DashboardHeader';

const Dashboard = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [debouncedSearchTerm, setDebouncedSearchTerm] = useState('');
  const [selectedFolderId, setSelectedFolderId] = useState<string | undefined>();
  const [isPhotoOnly, setIsPhotoOnly] = useState(false);
  const [isDocumentOnly, setIsDocumentOnly] = useState(false);

  // Debounce search term to avoid too many API calls
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearchTerm(searchTerm);
    }, 300); // 300ms delay

    return () => clearTimeout(timer);
  }, [searchTerm]);

  // Stable callbacks to prevent unnecessary re-renders
  const handleSearchChange = useCallback((term: string) => {
    setSearchTerm(term);
  }, []);

  const handlePhotoOnlyChange = useCallback((value: boolean) => {
    setIsPhotoOnly(value);
  }, []);

  const handleDocumentOnlyChange = useCallback((value: boolean) => {
    setIsDocumentOnly(value);
  }, []);

  const handleFolderSelect = useCallback((folderId: string | undefined) => {
    setSelectedFolderId(folderId);
  }, []);

  const { data: filesData, isLoading: filesLoading } = useQuery({
    queryKey: ['files', debouncedSearchTerm, selectedFolderId, isPhotoOnly],
    queryFn: () =>
      fileApi.getFiles({
        searchTerm: debouncedSearchTerm || undefined,
        folderId: selectedFolderId,
        isPhoto: isPhotoOnly || undefined,
      }),
    staleTime: 30000, // Consider data fresh for 30 seconds
    refetchOnWindowFocus: false, // Don't refetch when window regains focus
    placeholderData: (previousData) => previousData, // Keep previous data during transitions (replaces keepPreviousData in v5)
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

  const filteredFiles =
    filesData?.files?.filter((file: FileItemDto) => {
      if (isDocumentOnly) {
        return documentMimeTypes.some((mimeType) =>
          file.mimeType?.toLowerCase().startsWith(mimeType.toLowerCase()),
        );
      }
      return true;
    }) || [];

  const { data: foldersData, isLoading: foldersLoading } = useQuery({
    queryKey: ['folders'],
    queryFn: () => folderApi.getFolders(),
  });

  return (
    <div
      style={{
        display: 'flex',
        height: '100vh',
        flexDirection: 'column',
        backgroundColor: 'var(--bg-primary)',
        fontFamily:
          '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif',
      }}
    >
      <DashboardHeader
        searchTerm={searchTerm}
        onSearchChange={handleSearchChange}
        isPhotoOnly={isPhotoOnly}
        onPhotoOnlyChange={handlePhotoOnlyChange}
        isDocumentOnly={isDocumentOnly}
        onDocumentOnlyChange={handleDocumentOnlyChange}
      />

      <div className='dashboard-layout'>
        <aside className='dashboard-sidebar'>
          <FolderTree
            folders={foldersData?.folders || []}
            onFolderSelect={handleFolderSelect}
            selectedFolderId={selectedFolderId}
            isLoading={foldersLoading}
          />
        </aside>

        <main className='dashboard-main'>
          <FileUpload destinationFolderId={selectedFolderId} />
          <FileList
            files={filteredFiles}
            isLoading={filesLoading && !filesData} // Only show loading if we don't have previous data
            totalCount={isDocumentOnly ? filteredFiles.length : filesData?.totalCount || 0}
          />
        </main>
      </div>
    </div>
  );
};

export default Dashboard;

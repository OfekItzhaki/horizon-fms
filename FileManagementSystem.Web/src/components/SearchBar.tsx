import { useState, useEffect, useCallback, memo } from 'react';
import './SearchBar.css';

interface SearchBarProps {
  searchTerm: string;
  onSearchChange: (term: string) => void;
  isPhotoOnly: boolean;
  onPhotoOnlyChange: (value: boolean) => void;
  isDocumentOnly: boolean;
  onDocumentOnlyChange: (value: boolean) => void;
}

const SearchBar = memo(({
  searchTerm,
  onSearchChange,
  isPhotoOnly,
  onPhotoOnlyChange,
  isDocumentOnly,
  onDocumentOnlyChange
}: SearchBarProps) => {
  const [isMobile, setIsMobile] = useState(window.innerWidth <= 768);
  const [localSearchTerm, setLocalSearchTerm] = useState(searchTerm);

  useEffect(() => {
    const handleResize = () => {
      setIsMobile(window.innerWidth <= 768);
    };
    window.addEventListener('resize', handleResize);
    return () => window.removeEventListener('resize', handleResize);
  }, []);

  // Debounce local search term and pass to parent
  useEffect(() => {
    const timer = setTimeout(() => {
      onSearchChange(localSearchTerm);
    }, 300);

    return () => clearTimeout(timer);
  }, [localSearchTerm, onSearchChange]);

  // Sync local state with prop when it changes externally (e.g., from Clear button)
  useEffect(() => {
    if (searchTerm !== localSearchTerm) {
      setLocalSearchTerm(searchTerm);
    }
  }, [searchTerm]);

  const handleInputChange = useCallback((value: string) => {
    setLocalSearchTerm(value);
  }, []);

  const handleClear = useCallback(() => {
    setLocalSearchTerm('');
    onSearchChange('');
  }, [onSearchChange]);

  return (
    <div className={`search-bar-container ${isMobile ? 'mobile' : ''}`}>
      <div className="search-input-wrapper">
        <div className="input-container">
          <input
            type="text"
            placeholder="Search files..."
            className="main-search-input"
            value={localSearchTerm}
            onChange={(e) => handleInputChange(e.target.value)}
            style={{ paddingRight: localSearchTerm ? '2.75rem' : '1rem' }}
          />
          <span className="search-icon">🔍</span>
          {localSearchTerm && (
            <button
              onClick={(e) => {
                e.stopPropagation();
                handleClear();
              }}
              className="clear-search-btn"
              title="Clear search"
            >
              ×
            </button>
          )}
        </div>
      </div>
      <div className="filter-controls">
        <label className="filter-label">
          <input
            type="checkbox"
            className="filter-checkbox"
            checked={isPhotoOnly}
            onChange={(e) => onPhotoOnlyChange(e.target.checked)}
          />
          Photos
        </label>
        <label className="filter-label">
          <input
            type="checkbox"
            className="filter-checkbox"
            checked={isDocumentOnly}
            onChange={(e) => onDocumentOnlyChange(e.target.checked)}
          />
          Documents
        </label>
      </div>
    </div>
  );
}, (prevProps, nextProps) => {
  // Only re-render if filter props change, not when searchTerm changes
  return prevProps.isPhotoOnly === nextProps.isPhotoOnly &&
    prevProps.isDocumentOnly === nextProps.isDocumentOnly &&
    prevProps.onSearchChange === nextProps.onSearchChange &&
    prevProps.onPhotoOnlyChange === nextProps.onPhotoOnlyChange &&
    prevProps.onDocumentOnlyChange === nextProps.onDocumentOnlyChange;
});

SearchBar.displayName = 'SearchBar';

export default SearchBar;

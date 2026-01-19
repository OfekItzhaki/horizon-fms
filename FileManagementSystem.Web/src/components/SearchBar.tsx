interface SearchBarProps {
  searchTerm: string;
  onSearchChange: (term: string) => void;
  isPhotoOnly: boolean;
  onPhotoOnlyChange: (value: boolean) => void;
  isDocumentOnly: boolean;
  onDocumentOnlyChange: (value: boolean) => void;
}

const SearchBar = ({ 
  searchTerm, 
  onSearchChange, 
  isPhotoOnly, 
  onPhotoOnlyChange,
  isDocumentOnly,
  onDocumentOnlyChange
}: SearchBarProps) => {
  return (
    <div style={{ display: 'flex', gap: '1rem', alignItems: 'center', marginTop: '0.5rem' }}>
      <input
        type="text"
        placeholder="Search files..."
        value={searchTerm}
        onChange={(e) => onSearchChange(e.target.value)}
        style={{
          padding: '0.5rem',
          borderRadius: '4px',
          border: '1px solid #ccc',
          flex: 1,
          maxWidth: '400px',
        }}
      />
      <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
        <input
          type="checkbox"
          checked={isPhotoOnly}
          onChange={(e) => onPhotoOnlyChange(e.target.checked)}
        />
        Photos Only
      </label>
      <label style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
        <input
          type="checkbox"
          checked={isDocumentOnly}
          onChange={(e) => onDocumentOnlyChange(e.target.checked)}
        />
        Documents Only
      </label>
      {searchTerm && (
        <button
          onClick={() => onSearchChange('')}
          style={{
            padding: '0.5rem 1rem',
            borderRadius: '4px',
            border: '1px solid #ccc',
            background: '#fff',
            cursor: 'pointer',
          }}
        >
          Clear
        </button>
      )}
    </div>
  );
};

export default SearchBar;

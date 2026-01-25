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
    <div style={{ 
      display: 'flex', 
      gap: '1rem', 
      alignItems: 'center', 
      flexWrap: 'wrap'
    }}>
      <div style={{ position: 'relative', flex: 1, maxWidth: '500px' }}>
        <input
          type="text"
          placeholder="Search files..."
          value={searchTerm}
          onChange={(e) => onSearchChange(e.target.value)}
          style={{
            padding: '0.75rem 1rem 0.75rem 2.5rem',
            borderRadius: '8px',
            border: '1px solid rgba(255, 255, 255, 0.3)',
            background: 'rgba(255, 255, 255, 0.15)',
            backdropFilter: 'blur(10px)',
            color: '#ffffff',
            fontSize: '0.95rem',
            width: '100%',
            transition: 'all 0.2s',
            outline: 'none',
          }}
          onFocus={(e) => {
            e.target.style.background = 'rgba(255, 255, 255, 0.25)';
            e.target.style.borderColor = 'rgba(255, 255, 255, 0.5)';
          }}
          onBlur={(e) => {
            e.target.style.background = 'rgba(255, 255, 255, 0.15)';
            e.target.style.borderColor = 'rgba(255, 255, 255, 0.3)';
          }}
        />
        <span style={{
          position: 'absolute',
          left: '0.75rem',
          top: '50%',
          transform: 'translateY(-50%)',
          fontSize: '1.1rem',
          color: 'rgba(255, 255, 255, 0.7)'
        }}>🔍</span>
      </div>
      <label style={{ 
        display: 'flex', 
        alignItems: 'center', 
        gap: '0.5rem',
        color: '#ffffff',
        fontSize: '0.9rem',
        fontWeight: '500',
        cursor: 'pointer',
        padding: '0.5rem 0.75rem',
        borderRadius: '6px',
        background: 'rgba(255, 255, 255, 0.1)',
        transition: 'all 0.2s'
      }}
      onMouseEnter={(e) => {
        e.currentTarget.style.background = 'rgba(255, 255, 255, 0.2)';
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.background = 'rgba(255, 255, 255, 0.1)';
      }}
      >
        <input
          type="checkbox"
          checked={isPhotoOnly}
          onChange={(e) => onPhotoOnlyChange(e.target.checked)}
          style={{
            width: '18px',
            height: '18px',
            cursor: 'pointer',
            accentColor: '#ffffff'
          }}
        />
        Photos
      </label>
      <label style={{ 
        display: 'flex', 
        alignItems: 'center', 
        gap: '0.5rem',
        color: '#ffffff',
        fontSize: '0.9rem',
        fontWeight: '500',
        cursor: 'pointer',
        padding: '0.5rem 0.75rem',
        borderRadius: '6px',
        background: 'rgba(255, 255, 255, 0.1)',
        transition: 'all 0.2s'
      }}
      onMouseEnter={(e) => {
        e.currentTarget.style.background = 'rgba(255, 255, 255, 0.2)';
      }}
      onMouseLeave={(e) => {
        e.currentTarget.style.background = 'rgba(255, 255, 255, 0.1)';
      }}
      >
        <input
          type="checkbox"
          checked={isDocumentOnly}
          onChange={(e) => onDocumentOnlyChange(e.target.checked)}
          style={{
            width: '18px',
            height: '18px',
            cursor: 'pointer',
            accentColor: '#ffffff'
          }}
        />
        Documents
      </label>
      {searchTerm && (
        <button
          onClick={() => onSearchChange('')}
          style={{
            padding: '0.5rem 1rem',
            borderRadius: '6px',
            border: '1px solid rgba(255, 255, 255, 0.3)',
            background: 'rgba(255, 255, 255, 0.15)',
            color: '#ffffff',
            cursor: 'pointer',
            fontSize: '0.9rem',
            fontWeight: '500',
            transition: 'all 0.2s'
          }}
          onMouseEnter={(e) => {
            e.currentTarget.style.background = 'rgba(255, 255, 255, 0.25)';
            e.currentTarget.style.borderColor = 'rgba(255, 255, 255, 0.5)';
          }}
          onMouseLeave={(e) => {
            e.currentTarget.style.background = 'rgba(255, 255, 255, 0.15)';
            e.currentTarget.style.borderColor = 'rgba(255, 255, 255, 0.3)';
          }}
        >
          Clear
        </button>
      )}
    </div>
  );
};

export default SearchBar;

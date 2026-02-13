import { useTheme } from '../context/ThemeContextCore';
import { Sun, Moon } from 'lucide-react';

export default function ThemeToggle() {
  const { theme, toggleTheme } = useTheme();

  return (
    <button
      onClick={toggleTheme}
      className='px-4 py-2 rounded-lg hover:bg-[var(--surface-secondary)] transition-all duration-200'
      title={`Switch to ${theme === 'light' ? 'dark' : 'light'} mode`}
      style={{
        background: 'rgba(255, 255, 255, 0.1)',
        border: '1px solid rgba(255, 255, 255, 0.2)',
        color: '#ffffff',
        display: 'flex',
        alignItems: 'center',
        gap: '8px',
        fontWeight: '500',
        fontSize: '14px',
        whiteSpace: 'nowrap',
      }}
    >
      {theme === 'light' ? <Moon size={18} /> : <Sun size={18} />}
      <span>{theme === 'light' ? 'Dark' : 'Light'} Mode</span>
    </button>
  );
}

import React from 'react';
import './LoadingSpinner.css';

interface LoadingSpinnerProps {
  size?: 'sm' | 'md' | 'lg';
  color?: string;
  className?: string;
}

export const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({
  size = 'md',
  color = 'var(--primary-color)',
  className = '',
}) => {
  const sizeClass = `spinner-${size}`;

  return (
    <div className={`spinner-container ${className}`}>
      <div
        className={`spinner ${sizeClass}`}
        style={{ borderColor: `${color} transparent transparent transparent` }}
        role='status'
        aria-label='Loading'
      />
    </div>
  );
};

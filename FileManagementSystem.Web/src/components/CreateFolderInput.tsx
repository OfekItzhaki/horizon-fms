import { useState } from 'react';

interface CreateFolderInputProps {
    onSave: (name: string) => void;
    onCancel: () => void;
    isPending: boolean;
}

export const CreateFolderInput = ({ onSave, onCancel, isPending }: CreateFolderInputProps) => {
    const [name, setName] = useState('');

    const handleSave = () => {
        if (name.trim()) {
            onSave(name.trim());
        }
    };

    return (
        <div className="create-input-container">
            <input
                className="folder-input"
                placeholder="Folder name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                onKeyDown={(e) => {
                    if (e.key === 'Enter') handleSave();
                    else if (e.key === 'Escape') onCancel();
                }}
                autoFocus
            />
            <div className="create-actions">
                <button
                    className="btn-primary"
                    onClick={handleSave}
                    disabled={isPending || !name.trim()}
                >
                    {isPending ? 'Creating...' : 'Create'}
                </button>
                <button className="btn-secondary" onClick={onCancel}>
                    Cancel
                </button>
            </div>
        </div>
    );
};

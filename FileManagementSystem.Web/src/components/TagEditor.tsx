import { useState, useEffect } from 'react';
import { X, Plus, Save, Loader2 } from 'lucide-react';
import { fileApi } from '../services/api';
import { toast } from 'react-hot-toast';

interface TagEditorProps {
  fileId: string;
  initialTags: string[];
  isOpen: boolean;
  onClose: () => void;
  onTagsUpdated: (newTags: string[]) => void;
}

export default function TagEditor({
  fileId,
  initialTags,
  isOpen,
  onClose,
  onTagsUpdated,
}: TagEditorProps) {
  const [tags, setTags] = useState<string[]>(initialTags);
  const [newTag, setNewTag] = useState('');
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    setTags(initialTags);
  }, [initialTags, isOpen]);

  if (!isOpen) return null;

  const handleAddTag = (e?: React.FormEvent) => {
    e?.preventDefault();
    const trimmed = newTag.trim();
    if (trimmed && !tags.includes(trimmed)) {
      setTags([...tags, trimmed]);
      setNewTag('');
    }
  };

  const handleRemoveTag = (tagToRemove: string) => {
    setTags(tags.filter((tag) => tag !== tagToRemove));
  };

  const handleSave = async () => {
    setIsSaving(true);
    try {
      await fileApi.setTags(fileId, tags);
      toast.success('Tags updated successfully');
      onTagsUpdated(tags);
      onClose();
    } catch (error) {
      toast.error('Failed to update tags');
      console.error(error);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div
      className='fixed inset-0 bg-black/40 flex items-center justify-center z-50 backdrop-blur-md transition-all duration-300'
      onClick={onClose}
    >
      <div
        className='bg-[var(--surface-primary)] p-6 rounded-2xl shadow-2xl w-full max-w-md border border-[var(--border-color)] transform transition-all'
        onClick={(e) => e.stopPropagation()}
      >
        <div className='flex justify-between items-center mb-6'>
          <div>
            <h3 className='text-xl font-semibold text-[var(--text-primary)]'>Manage Tags</h3>
            <p className='text-sm text-[var(--text-tertiary)] mt-1'>Organize your file with descriptive tags</p>
          </div>
          <button
            onClick={onClose}
            className='text-[var(--text-tertiary)] hover:text-[var(--text-primary)] hover:bg-[var(--surface-secondary)] p-2 rounded-xl transition-all'
          >
            <X size={20} />
          </button>
        </div>

        <div className='mb-6'>
          <label className='text-xs font-semibold text-[var(--text-tertiary)] uppercase tracking-wider mb-2 block'>
            Current Tags
          </label>
          <div className='flex flex-wrap gap-2 min-h-[48px] p-3 bg-[var(--bg-secondary)] rounded-xl border border-[var(--border-color)]'>
            {tags.length === 0 ? (
              <span className='text-[var(--text-tertiary)] text-sm italic py-1'>No tags added yet...</span>
            ) : (
              tags.map((tag) => (
                <span
                  key={tag}
                  className='inline-flex items-center px-3 py-1.5 rounded-lg text-sm font-medium bg-[var(--accent-primary)]/10 text-[var(--accent-primary)] border border-[var(--accent-primary)]/20 group transition-all hover:bg-[var(--accent-primary)]/20'
                >
                  {tag}
                  <button
                    onClick={() => handleRemoveTag(tag)}
                    className='ml-2 text-[var(--accent-primary)]/60 hover:text-red-500 transition-colors'
                    title={`Remove ${tag}`}
                  >
                    <X size={14} />
                  </button>
                </span>
              ))
            )}
          </div>
        </div>

        <form onSubmit={handleAddTag} className='mb-8'>
          <label className='text-xs font-semibold text-[var(--text-tertiary)] uppercase tracking-wider mb-2 block'>
            Add New Tag
          </label>
          <div className='flex gap-2'>
            <div className='relative flex-1'>
              <input
                type='text'
                value={newTag}
                onChange={(e) => setNewTag(e.target.value)}
                placeholder='e.g. project, invoice, urgent'
                className='w-full px-4 py-2.5 rounded-xl bg-[var(--bg-primary)] border border-[var(--border-color)] text-[var(--text-primary)] placeholder:text-[var(--text-tertiary)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-primary)]/30 focus:border-[var(--accent-primary)] transition-all'
              />
            </div>
            <button
              type='submit'
              disabled={!newTag.trim()}
              className='px-4 rounded-xl bg-[var(--accent-primary)] text-white hover:bg-[var(--accent-secondary)] disabled:opacity-40 disabled:cursor-not-allowed transition-all shadow-lg shadow-[var(--accent-primary)]/20 flex items-center justify-center'
            >
              <Plus size={20} />
            </button>
          </div>
        </form>

        <div className='flex justify-end gap-3 pt-4 border-t border-[var(--border-color)]'>
          <button
            onClick={onClose}
            className='px-5 py-2.5 rounded-xl text-[var(--text-secondary)] hover:bg-[var(--surface-secondary)] font-medium transition-all'
          >
            Cancel
          </button>
          <button
            onClick={handleSave}
            disabled={isSaving}
            className='px-6 py-2.5 rounded-xl bg-[var(--accent-primary)] text-white hover:bg-[var(--accent-secondary)] flex items-center gap-2 font-semibold transition-all shadow-lg shadow-[var(--accent-primary)]/25 disabled:opacity-70'
          >
            {isSaving ? <Loader2 className='animate-spin' size={18} /> : <Save size={18} />}
            Save Changes
          </button>
        </div>
      </div>
    </div>
  );
}

import * as React from 'react';
import { useState } from 'react';
import { Button, ButtonSize } from './Button';

interface ConfirmButtonProps {
  onConfirm: () => void | Promise<void>;
  children: React.ReactNode;
  confirmLabel?: string;
  cancelLabel?: string;
  prompt?: string;
  size?: ButtonSize;
  className?: string;
}

/** Two-step destructive action: first click arms it, second click fires. */
export function ConfirmButton({
  onConfirm,
  children,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  prompt,
  size = 'md',
  className = '',
}: ConfirmButtonProps) {
  const [armed, setArmed] = useState(false);
  const [busy, setBusy] = useState(false);

  if (!armed) {
    return (
      <Button variant="outline-danger" size={size} className={className} onClick={() => setArmed(true)}>
        {children}
      </Button>
    );
  }

  const handleConfirm = async () => {
    setBusy(true);
    try {
      await onConfirm();
    } finally {
      setBusy(false);
      setArmed(false);
    }
  };

  return (
    <div className={`flex flex-col gap-2 ${className}`}>
      {prompt && <p className="font-mono text-xs text-danger">{prompt}</p>}
      <div className="flex gap-2">
        <Button variant="danger" size={size} className="flex-1" onClick={handleConfirm} disabled={busy}>
          {busy ? 'Working...' : confirmLabel}
        </Button>
        <Button variant="ghost" size={size} className="flex-1" onClick={() => setArmed(false)} disabled={busy}>
          {cancelLabel}
        </Button>
      </div>
    </div>
  );
}

import * as React from 'react';

interface AlertProps {
  tone: 'success' | 'error';
  children: React.ReactNode;
  className?: string;
}

const toneClass = {
  success: 'alert-success',
  error: 'alert-error',
};

export function Alert({ tone, children, className = '' }: AlertProps) {
  return (
    <div role={tone === 'error' ? 'alert' : 'status'} className={`alert ${toneClass[tone]} ${className}`}>
      <span className="flex-shrink-0 font-bold">{tone === 'error' ? '[ERR]' : '[OK]'}</span>
      <div className="min-w-0 flex-1">{children}</div>
    </div>
  );
}

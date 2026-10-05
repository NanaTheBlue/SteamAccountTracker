import * as React from 'react';

interface BadgeProps {
  tone: 'clean' | 'danger' | 'info' | 'primary';
  children: React.ReactNode;
  className?: string;
}

const toneClass = {
  clean: 'badge-clean',
  danger: 'badge-danger',
  info: 'badge-info',
  primary: 'badge-primary',
};

export function Badge({ tone, children, className = '' }: BadgeProps) {
  return <span className={`badge ${toneClass[tone]} ${className}`}>{children}</span>;
}

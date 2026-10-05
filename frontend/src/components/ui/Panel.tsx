import * as React from 'react';

interface PanelProps extends React.HTMLAttributes<HTMLDivElement> {
  tone?: 'default' | 'danger' | 'clean';
  title?: string;
}

const toneClass = {
  default: '',
  danger: 'panel-danger',
  clean: 'panel-clean',
};

export function Panel({ tone = 'default', title, className = '', children, ...rest }: PanelProps) {
  return (
    <div className={`panel p-6 ${toneClass[tone]} ${className}`} {...rest}>
      {title && <h2 className="hud-title mb-5">{title}</h2>}
      {children}
    </div>
  );
}

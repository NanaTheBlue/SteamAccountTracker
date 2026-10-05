import * as React from 'react';

interface PageHeaderProps {
  /** Small mono line above the title, e.g. a fake file path. */
  eyebrow?: string;
  title: string;
  subtitle?: React.ReactNode;
  action?: React.ReactNode;
}

export function PageHeader({ eyebrow, title, subtitle, action }: PageHeaderProps) {
  return (
    <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div className="min-w-0">
        {eyebrow && <p className="mono-label mb-2">{eyebrow}</p>}
        <h1 className="text-3xl sm:text-4xl text-fg">{title}</h1>
        {subtitle && <p className="mt-2 text-muted">{subtitle}</p>}
      </div>
      {action && <div className="flex-shrink-0">{action}</div>}
    </div>
  );
}

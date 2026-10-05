import * as React from 'react';
import { Link } from 'react-router';
import { Panel } from './ui/Panel';

interface AuthShellProps {
  eyebrow: string;
  title: string;
  children: React.ReactNode;
  footerPrompt: string;
  footerLinkTo: string;
  footerLinkLabel: string;
}

/** Shared frame for the login and register screens. */
export function AuthShell({ eyebrow, title, children, footerPrompt, footerLinkTo, footerLinkLabel }: AuthShellProps) {
  return (
    <div className="flex min-h-[80vh] flex-col justify-center px-4 py-12">
      <div className="mx-auto w-full max-w-md">
        <p className="mono-label mb-3 text-center">{eyebrow}</p>
        <h2 className="text-center text-4xl text-fg">{title}</h2>

        <Panel className="mt-8 px-6 py-8 sm:px-10">
          {children}

          <p className="mt-8 border-t border-line pt-6 text-center font-mono text-xs text-muted">
            {footerPrompt}{' '}
            <Link to={footerLinkTo} className="text-primary hover:underline">
              {footerLinkLabel}
            </Link>
          </p>
        </Panel>
      </div>
    </div>
  );
}

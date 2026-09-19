import * as React from 'react';
import { createRoot } from 'react-dom/client';
import * as Sentry from '@sentry/react';
import './index.css';
import App from './App';

if (import.meta.env.VITE_SENTRY_DSN) {
  Sentry.init({
    dsn: import.meta.env.VITE_SENTRY_DSN,
    integrations: [
      Sentry.browserTracingIntegration(),
      Sentry.replayIntegration(),
    ],
    tracesSampleRate: 1.0, 
    replaysSessionSampleRate: 0.1,
    replaysOnErrorSampleRate: 1.0,
  });
}

const container = document.getElementById('root');
if (!container) {
  throw new Error('Failed to find the root element');
}
const root = createRoot(container);
root.render(
  <React.StrictMode>
    <Sentry.ErrorBoundary fallback={<div className="p-8 text-center text-red-500">An unexpected error occurred. Please refresh the page.</div>}>
      <App />
    </Sentry.ErrorBoundary>
  </React.StrictMode>
);

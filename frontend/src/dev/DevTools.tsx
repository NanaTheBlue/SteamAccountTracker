/**
 * DEV-ONLY UI. Only reachable through `import.meta.env.DEV`-guarded lazy
 * imports, so none of this ships in production builds.
 */
import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router';
import { useAuth } from '../context/AuthContext';
import { Button } from '../components/ui/Button';
import { DEV_TEST_USER, MOCK_CHANGE_EVENT, isMockMode, resetMockDb, setMockMode } from './mockApi';

/** One-click login as a test operator against the mock API. */
export function DevLoginPanel() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [busy, setBusy] = useState(false);

  const handleClick = async () => {
    setBusy(true);
    setMockMode(true);
    try {
      await login(DEV_TEST_USER.email, DEV_TEST_USER.password);
      navigate('/dashboard');
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="mt-6 border border-dashed border-info/50 bg-info/5 p-4">
      <p className="mb-3 font-mono text-[11px] uppercase tracking-widest text-info">
        // local dev only · not in production builds
      </p>
      <Button variant="ghost" className="w-full" onClick={handleClick} disabled={busy}>
        {busy ? 'Spawning...' : 'Log in as test operator (mock API)'}
      </Button>
    </div>
  );
}

/** Floating indicator shown while requests are served by the mock API. */
export function DevMockBadge() {
  const [on, setOn] = useState(isMockMode);

  useEffect(() => {
    const sync = () => setOn(isMockMode());
    window.addEventListener(MOCK_CHANGE_EVENT, sync);
    window.addEventListener('storage', sync);
    return () => {
      window.removeEventListener(MOCK_CHANGE_EVENT, sync);
      window.removeEventListener('storage', sync);
    };
  }, []);

  if (!on) return null;

  return (
    <div className="fixed bottom-4 left-4 z-50 flex items-center gap-3 border border-info/60 bg-canvas/95 px-3 py-2 font-mono text-[11px] uppercase tracking-widest text-info shadow-lg">
      <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-info" aria-hidden="true" />
      Mock API
      <button
        type="button"
        className="text-dim underline-offset-2 hover:text-fg hover:underline"
        onClick={() => {
          resetMockDb();
          window.location.reload();
        }}
      >
        reset data
      </button>
    </div>
  );
}

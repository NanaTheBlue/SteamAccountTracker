import { useState, useEffect, useRef } from 'react';
import { apiGet, apiDelete, ApiError } from '../api/client';
import { AccountCard, TrackedAccountDto, isAccountBanned } from '../components/AccountCard';
import { Alert } from '../components/ui/Alert';
import { Button } from '../components/ui/Button';
import { PageHeader } from '../components/ui/PageHeader';
import { Panel } from '../components/ui/Panel';
import { Spinner } from '../components/ui/Spinner';

export function Dashboard() {
  const [accounts, setAccounts] = useState<TrackedAccountDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const messageTimer = useRef<ReturnType<typeof setTimeout>>(undefined);
  const errorTimer = useRef<ReturnType<typeof setTimeout>>(undefined);

  useEffect(() => {
    fetchAccounts();
    return () => {
      clearTimeout(messageTimer.current);
      clearTimeout(errorTimer.current);
    };
  }, []);

  // Each new toast replaces the previous one's timer, so an older timeout
  // can't wipe a newer message early.
  const flashMessage = (text: string) => {
    clearTimeout(messageTimer.current);
    setMessage(text);
    messageTimer.current = setTimeout(() => setMessage(null), 3000);
  };

  const flashError = (text: string) => {
    clearTimeout(errorTimer.current);
    setError(text);
    errorTimer.current = setTimeout(() => setError(null), 5000);
  };

  const fetchAccounts = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await apiGet<{ accounts: TrackedAccountDto[] }>('/api/steam/tracked');
      setAccounts(data.accounts || []);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Failed to fetch tracked accounts.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  const handleUntrack = async (steamId64: string) => {
    try {
      await apiDelete(`/api/steam/track/${steamId64}`);
      setAccounts(prev => prev.filter(a => a.steamId64 !== steamId64));
      flashMessage(`Dropped ${steamId64}. You'll no longer get alerts for this account.`);
    } catch (err) {
      flashError(err instanceof ApiError ? err.message : 'Failed to untrack account.');
    }
  };

  const bannedCount = accounts.filter(isAccountBanned).length;
  const stats = [
    { label: 'Tracked', value: accounts.length, tone: 'text-fg' },
    { label: 'Banned', value: bannedCount, tone: 'text-danger' },
    { label: 'Still at large', value: accounts.length - bannedCount, tone: 'text-primary' },
  ];

  return (
    <div className="mx-auto max-w-6xl px-4 py-10">
      <PageHeader
        eyebrow="// watchlist"
        title="Suspect files"
        subtitle="Everyone you've flagged, and whether Valve has caught up with them yet."
        action={<Button to="/track">+ Flag a suspect</Button>}
      />

      {!isLoading && accounts.length > 0 && (
        <div className="mb-8 grid grid-cols-3 gap-px border border-line bg-line">
          {stats.map(s => (
            <div key={s.label} className="bg-surface px-4 py-3">
              <p className="mono-label">{s.label}</p>
              <p className={`font-display text-3xl font-bold ${s.tone}`}>{s.value}</p>
            </div>
          ))}
        </div>
      )}

      {message && <Alert tone="success" className="mb-6">{message}</Alert>}
      {error && <Alert tone="error" className="mb-6">{error}</Alert>}

      {isLoading ? (
        <Spinner className="py-16" label="Pulling suspect files" />
      ) : accounts.length === 0 ? (
        <Panel className="py-16 text-center">
          <p className="font-display text-2xl font-bold uppercase tracking-wider text-fg">
            Lobby's suspiciously clean.
          </p>
          <p className="mb-6 mt-2 text-muted">You haven't flagged anyone yet. Got a name from your last match?</p>
          <Button to="/track">Flag your first suspect</Button>
        </Panel>
      ) : (
        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {accounts.map(account => (
            <AccountCard
              key={account.steamId64}
              account={account}
              onUntrack={handleUntrack}
            />
          ))}
        </div>
      )}
    </div>
  );
}

import * as React from 'react';
import { useState } from 'react';
import { apiPost, ApiError } from '../api/client';
import { Alert } from '../components/ui/Alert';
import { Button } from '../components/ui/Button';
import { PageHeader } from '../components/ui/PageHeader';
import { Panel } from '../components/ui/Panel';

export function TrackAccount() {
  const [steamInput, setSteamInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!steamInput.trim()) return;

    setIsLoading(true);
    setError(null);
    setSuccessMsg(null);

    try {
      const data = await apiPost<{ message: string; steamId64: string }>('/api/steam/track', { steamInput });
      setSuccessMsg(`Target acquired: ${data.steamId64}. If Valve bans them, you'll hear about it.`);
      setSteamInput('');
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('An unexpected error occurred while tracking the account.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="mx-auto max-w-2xl px-4 py-12">
      <PageHeader
        eyebrow="// new target"
        title="Flag a suspect"
        subtitle="Add the account to your watchlist. We check its public ban status and alert you if Valve acts."
      />

      <Panel>
        {error && <Alert tone="error" className="mb-6">{error}</Alert>}

        {successMsg && (
          <Alert tone="success" className="mb-6">
            <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-center">
              <span>{successMsg}</span>
              <Button to="/dashboard" size="sm" variant="ghost">
                View suspects
              </Button>
            </div>
          </Alert>
        )}

        <form onSubmit={handleSubmit} className="space-y-6">
          <div>
            <label htmlFor="steamInput" className="field-label">
              Steam profile link or ID
            </label>
            <input
              id="steamInput"
              type="text"
              required
              maxLength={300}
              autoComplete="off"
              spellCheck={false}
              value={steamInput}
              onChange={(e) => setSteamInput(e.target.value)}
              placeholder="https://steamcommunity.com/id/..."
              className="field-input py-3 text-base"
            />
            <p className="field-hint">
              Paste their profile link from Steam. A SteamID64 or STEAM_X:Y:Z works too.
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-4">
            <Button type="submit" disabled={isLoading || !steamInput.trim()}>
              {isLoading ? 'Acquiring...' : 'Track account'}
            </Button>
            <Button to="/dashboard" variant="ghost">
              Back to suspects
            </Button>
          </div>
        </form>

        <p className="mt-6 border-t border-line pt-4 font-mono text-xs text-dim">
          Heads up: tracking doesn't report anyone or speed up a ban. If they're cheating, report them in-game too.
        </p>
      </Panel>
    </div>
  );
}

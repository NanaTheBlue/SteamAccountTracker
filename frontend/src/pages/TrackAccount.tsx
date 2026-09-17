import * as React from 'react';
import { useState } from 'react';
import { Link } from 'react-router';
import { apiPost, ApiError } from '../api/client';

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
      setSuccessMsg(`Successfully tracked account with SteamID64: ${data.steamId64}`);
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
    <div className="max-w-2xl mx-auto px-4 py-12">
      <div className="bg-gray-800 rounded-lg p-8 border border-gray-700 shadow-lg">
        <h2 className="text-2xl font-bold text-white mb-6">Track a Steam Account</h2>
        
        {error && (
          <div className="mb-6 bg-red-900/50 border border-red-500 text-red-200 px-4 py-3 rounded text-sm">
            {error}
          </div>
        )}

        {successMsg && (
          <div className="mb-6 bg-green-900/50 border border-green-500 text-green-200 px-4 py-3 rounded text-sm flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <span>{successMsg}</span>
            <Link 
              to="/"
              className="text-center px-4 py-2 bg-green-800 hover:bg-green-700 text-white rounded text-sm font-medium transition-colors"
            >
              View Dashboard
            </Link>
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-6">
          <div>
            <label htmlFor="steamInput" className="block text-sm font-medium text-gray-300 mb-2">
              Enter a SteamID64, profile URL, vanity URL, or STEAM_X:Y:Z
            </label>
            <input
              id="steamInput"
              type="text"
              required
              value={steamInput}
              onChange={(e) => setSteamInput(e.target.value)}
              placeholder="e.g. 76561197960287930"
              className="appearance-none block w-full px-4 py-3 border border-gray-600 rounded-md shadow-sm bg-gray-900 text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            />
          </div>

          <div className="flex items-center gap-4">
            <button
              type="submit"
              disabled={isLoading || !steamInput.trim()}
              className="px-6 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isLoading ? 'Tracking...' : 'Track Account'}
            </button>
            <Link 
              to="/"
              className="text-gray-400 hover:text-gray-300 text-sm font-medium"
            >
              Back to Dashboard
            </Link>
          </div>
        </form>
      </div>
    </div>
  );
}

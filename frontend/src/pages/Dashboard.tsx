import { useState, useEffect } from 'react';
import { Link } from 'react-router';
import { apiGet, apiDelete, ApiError } from '../api/client';
import { AccountCard, TrackedAccountDto } from '../components/AccountCard';

export function Dashboard() {
  const [accounts, setAccounts] = useState<TrackedAccountDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    fetchAccounts();
  }, []);

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
      setAccounts(accounts.filter(a => a.steamId64 !== steamId64));
      setMessage(`Successfully untracked account ${steamId64}`);
      setTimeout(() => setMessage(null), 3000);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Failed to untrack account.');
      }
      setTimeout(() => setError(null), 5000);
    }
  };

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      <div className="flex justify-between items-center mb-8">
        <h1 className="text-2xl font-bold text-white">Your Tracked Accounts</h1>
        <Link 
          to="/track" 
          className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium transition-colors"
        >
          Track New Account
        </Link>
      </div>

      {message && (
        <div className="mb-6 p-4 bg-green-900/50 border border-green-500 text-green-200 rounded-md">
          {message}
        </div>
      )}

      {error && (
        <div className="mb-6 p-4 bg-red-900/50 border border-red-500 text-red-200 rounded-md">
          {error}
        </div>
      )}

      {isLoading ? (
        <div className="flex justify-center py-12">
          <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-blue-500"></div>
        </div>
      ) : accounts.length === 0 ? (
        <div className="text-center py-16 bg-gray-800 rounded-lg border border-gray-700">
          <p className="text-gray-400 mb-4 text-lg">No tracked accounts yet.</p>
          <Link 
            to="/track" 
            className="text-blue-500 hover:text-blue-400 font-medium"
          >
            Start tracking!
          </Link>
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-6">
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

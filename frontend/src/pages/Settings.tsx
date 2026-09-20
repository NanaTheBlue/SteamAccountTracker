import * as React from 'react';
import { useState } from 'react';
import { useAuth } from '../context/AuthContext';
//import { apiPost } from '../api/client'; // Will need apiPut later

export function Settings() {
  const { user } = useAuth();
  const [discordWebhook, setDiscordWebhook] = useState('');
  const [isSaving, setIsSaving] = useState(false);
  const [message, setMessage] = useState<{type: 'success' | 'error', text: string} | null>(null);

  const handleSaveDiscord = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSaving(true);
    setMessage(null);
    try {
      // Stub for future API call
      // await apiPut('/api/user/settings', { discordWebhook });
      setTimeout(() => {
        setMessage({ type: 'success', text: 'Settings saved! (Backend integration coming soon)' });
        setIsSaving(false);
      }, 500);
    } catch (err: any) {
      setMessage({ type: 'error', text: err.message || 'Failed to save settings' });
      setIsSaving(false);
    }
  };

  if (!user) return null;

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      <h1 className="text-3xl font-bold text-white mb-8">Account Settings</h1>
      
      <div className="space-y-6">
        {/* Profile Section */}
        <div className="bg-gray-800 rounded-lg p-6 border border-gray-700">
          <h2 className="text-xl font-bold text-white mb-4">Profile</h2>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-400 mb-1">Username</label>
              <div className="text-white bg-gray-900 px-4 py-2 rounded-md border border-gray-700 cursor-not-allowed opacity-75">
                {user.username}
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-400 mb-1">Email Address</label>
              <div className="text-white bg-gray-900 px-4 py-2 rounded-md border border-gray-700 cursor-not-allowed opacity-75">
                {user.email}
              </div>
            </div>
          </div>
        </div>

        {/* Notifications Section */}
        <div className="bg-gray-800 rounded-lg p-6 border border-gray-700">
          <h2 className="text-xl font-bold text-white mb-2">Notifications</h2>
          <p className="text-gray-400 text-sm mb-6">Configure how you want to receive ban alerts.</p>
          
          <form onSubmit={handleSaveDiscord} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-300 mb-1">Discord Webhook URL</label>
              <input
                type="url"
                value={discordWebhook}
                onChange={(e) => setDiscordWebhook(e.target.value)}
                placeholder="https://discord.com/api/webhooks/..."
                className="w-full px-3 py-2 bg-gray-900 border border-gray-600 rounded-md text-white placeholder-gray-500 focus:outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
              />
              <p className="mt-2 text-xs text-gray-500">
                Paste a Discord channel webhook to receive instant messages when a tracked account gets banned.
              </p>
            </div>
            
            {message && (
              <div className={`p-3 rounded-md text-sm ${message.type === 'success' ? 'bg-green-900/50 text-green-200 border border-green-800' : 'bg-red-900/50 text-red-200 border border-red-800'}`}>
                {message.text}
              </div>
            )}

            <button
              type="submit"
              disabled={isSaving}
              className="bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-4 rounded-md transition-colors disabled:opacity-50"
            >
              {isSaving ? 'Saving...' : 'Save Settings'}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}

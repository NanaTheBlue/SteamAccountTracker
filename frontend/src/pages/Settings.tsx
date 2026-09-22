import { useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { apiGet, apiPut, apiPost, apiDelete } from '../api/client';

interface UserSettings {
  emailNotificationsEnabled: boolean;
  discordNotificationsEnabled: boolean;
}

interface Webhook {
  id: string;
  name: string;
  webhookUrl: string;
  createdAt: string;
}

export function Settings() {
  const { user } = useAuth();
  const [settings, setSettings] = useState<UserSettings | null>(null);
  const [webhooks, setWebhooks] = useState<Webhook[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // New webhook form
  const [newWebhookName, setNewWebhookName] = useState('');
  const [newWebhookUrl, setNewWebhookUrl] = useState('');
  const [addingWebhook, setAddingWebhook] = useState(false);

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      const [settingsRes, webhooksRes] = await Promise.all([
        apiGet<UserSettings>('/api/user/settings'),
        apiGet<Webhook[]>('/api/user/webhooks')
      ]);

      setSettings(settingsRes);
      setWebhooks(webhooksRes);
      setError(null);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleToggleSettings = async (field: keyof UserSettings) => {
    if (!settings) return;
    const newSettings = { ...settings, [field]: !settings[field] };
    
    // Optimistic update
    setSettings(newSettings);

    try {
      await apiPut('/api/user/settings', newSettings);
    } catch (err: any) {
      setError(err.message);
      // Revert on failure
      setSettings(settings);
    }
  };

  const handleAddWebhook = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newWebhookName || !newWebhookUrl) return;

    try {
      setAddingWebhook(true);
      const newWebhook = await apiPost<Webhook>('/api/user/webhooks', { 
        name: newWebhookName, 
        webhookUrl: newWebhookUrl 
      });

      setWebhooks([newWebhook, ...webhooks]);
      setNewWebhookName('');
      setNewWebhookUrl('');
    } catch (err: any) {
      setError(err.message);
    } finally {
      setAddingWebhook(false);
    }
  };

  const handleDeleteWebhook = async (id: string) => {
    if (!confirm('Are you sure you want to delete this webhook?')) return;

    try {
      await apiDelete(`/api/user/webhooks/${id}`);
      setWebhooks(webhooks.filter(w => w.id !== id));
    } catch (err: any) {
      setError(err.message);
    }
  };

  if (!user) return null;

  if (loading) {
    return (
      <div className="flex justify-center p-8">
        <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-blue-500"></div>
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto px-4 py-8 space-y-8">
      <div>
        <h1 className="text-3xl font-bold text-white mb-2">Account Settings</h1>
        <p className="text-gray-400">Manage how and where you receive ban alerts.</p>
      </div>

      {error && (
        <div className="p-4 bg-red-900/50 text-red-200 border border-red-800 rounded-lg">
          {error}
        </div>
      )}

      {settings && (
        <div className="bg-gray-800 rounded-lg p-6 border border-gray-700 space-y-6">
          <h2 className="text-xl font-bold text-white">Preferences</h2>
          
          <div className="flex items-center justify-between py-4 border-b border-gray-700">
            <div>
              <p className="font-medium text-white">Email Notifications</p>
              <p className="text-sm text-gray-400">Receive alerts at your registered email address.</p>
            </div>
            <button
              onClick={() => handleToggleSettings('emailNotificationsEnabled')}
              className={`${settings.emailNotificationsEnabled ? 'bg-blue-600' : 'bg-gray-600'} relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none`}
            >
              <span className={`${settings.emailNotificationsEnabled ? 'translate-x-5' : 'translate-x-0'} pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out`} />
            </button>
          </div>

          <div className="flex items-center justify-between py-4">
            <div>
              <p className="font-medium text-white">Discord Notifications</p>
              <p className="text-sm text-gray-400">Send alerts to your configured Discord webhooks.</p>
            </div>
            <button
              onClick={() => handleToggleSettings('discordNotificationsEnabled')}
              className={`${settings.discordNotificationsEnabled ? 'bg-blue-600' : 'bg-gray-600'} relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none`}
            >
              <span className={`${settings.discordNotificationsEnabled ? 'translate-x-5' : 'translate-x-0'} pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out`} />
            </button>
          </div>
        </div>
      )}

      <div className="bg-gray-800 rounded-lg p-6 border border-gray-700 space-y-6">
        <h2 className="text-xl font-bold text-white">Discord Webhooks</h2>
        <p className="text-sm text-gray-400">Configure webhooks to receive rich embedded alerts in your Discord servers.</p>

        <form onSubmit={handleAddWebhook} className="flex gap-4 items-end bg-gray-900 p-4 rounded-lg border border-gray-700">
          <div className="flex-1">
            <label className="block text-sm font-medium text-gray-300 mb-1">Server / Channel Name</label>
            <input
              type="text"
              required
              value={newWebhookName}
              onChange={(e) => setNewWebhookName(e.target.value)}
              placeholder="e.g. My CS2 Server - #alerts"
              className="w-full px-3 py-2 bg-gray-800 border border-gray-600 rounded-md text-white placeholder-gray-500 focus:outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
            />
          </div>
          <div className="flex-[2]">
            <label className="block text-sm font-medium text-gray-300 mb-1">Webhook URL</label>
            <input
              type="url"
              required
              value={newWebhookUrl}
              onChange={(e) => setNewWebhookUrl(e.target.value)}
              placeholder="https://discord.com/api/webhooks/..."
              className="w-full px-3 py-2 bg-gray-800 border border-gray-600 rounded-md text-white placeholder-gray-500 focus:outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
            />
          </div>
          <button
            type="submit"
            disabled={addingWebhook || !newWebhookName || !newWebhookUrl}
            className="bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 disabled:opacity-50 font-medium"
          >
            {addingWebhook ? 'Adding...' : 'Add Webhook'}
          </button>
        </form>

        <div className="space-y-4 mt-6">
          {webhooks.length === 0 ? (
            <p className="text-gray-500 text-sm text-center py-4">No webhooks configured yet.</p>
          ) : (
            webhooks.map((webhook) => (
              <div key={webhook.id} className="flex items-center justify-between p-4 border border-gray-700 bg-gray-900 rounded-lg">
                <div>
                  <p className="font-medium text-white">{webhook.name}</p>
                  <p className="text-sm text-gray-400 truncate max-w-[300px] sm:max-w-md">{webhook.webhookUrl}</p>
                </div>
                <button
                  onClick={() => handleDeleteWebhook(webhook.id)}
                  className="text-red-400 hover:text-red-300 text-sm font-medium transition-colors"
                >
                  Delete
                </button>
              </div>
            ))
          )}
        </div>
      </div>

      <div className="bg-gray-800 rounded-lg p-6 border border-red-900/50 space-y-4">
        <h2 className="text-xl font-bold text-red-500">Danger Zone</h2>
        <p className="text-sm text-gray-400">Permanently delete your account and all tracked data. This cannot be undone.</p>
        <button
          onClick={async () => {
            if (confirm('Are you absolutely sure you want to delete your account? This is irreversible.')) {
              try {
                await apiDelete('/api/user');
                window.location.href = '/';
              } catch (err: any) {
                setError(err.message);
              }
            }
          }}
          className="bg-red-600/20 text-red-500 border border-red-800 hover:bg-red-600 hover:text-white px-4 py-2 rounded-md font-medium transition-colors"
        >
          Delete Account
        </button>
      </div>
    </div>
  );
}

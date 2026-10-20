import { useEffect, useState } from 'react';
import { useAuth } from '../context/AuthContext';
import { apiGet, apiPut, apiPost, apiDelete } from '../api/client';
import { Alert } from '../components/ui/Alert';
import { Button } from '../components/ui/Button';
import { ConfirmButton } from '../components/ui/ConfirmButton';
import { Field } from '../components/ui/Field';
import { PageHeader } from '../components/ui/PageHeader';
import { Panel } from '../components/ui/Panel';
import { Spinner } from '../components/ui/Spinner';
import { Toggle } from '../components/ui/Toggle';

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
  const { user, clearSession } = useAuth();
  const [settings, setSettings] = useState<UserSettings | null>(null);
  const [webhooks, setWebhooks] = useState<Webhook[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // New webhook form
  const [newWebhookName, setNewWebhookName] = useState('');
  const [newWebhookUrl, setNewWebhookUrl] = useState('');
  const [addingWebhook, setAddingWebhook] = useState(false);

  // Change password form
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [changingPassword, setChangingPassword] = useState(false);
  const [passwordSuccess, setPasswordSuccess] = useState<string | null>(null);
  const [passwordError, setPasswordError] = useState<string | null>(null);

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
    const previous = settings[field];
    const newSettings: UserSettings = {
      emailNotificationsEnabled: settings.emailNotificationsEnabled,
      discordNotificationsEnabled: settings.discordNotificationsEnabled,
      [field]: !previous,
    };

    // Optimistic update
    setSettings(newSettings);
    setError(null);

    try {
      await apiPut('/api/user/settings', newSettings);
    } catch (err: any) {
      setError(err.message);
      // Revert only the field that failed, so a concurrent toggle isn't clobbered
      setSettings(s => (s ? { ...s, [field]: previous } : s));
    }
  };

  const handleAddWebhook = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newWebhookName || !newWebhookUrl) return;

    try {
      setAddingWebhook(true);
      setError(null);
      const newWebhook = await apiPost<Webhook>('/api/user/webhooks', { 
        name: newWebhookName, 
        webhookUrl: newWebhookUrl 
      });

      setWebhooks(prev => [newWebhook, ...prev]);
      setNewWebhookName('');
      setNewWebhookUrl('');
    } catch (err: any) {
      setError(err.message);
    } finally {
      setAddingWebhook(false);
    }
  };

  const handleDeleteAccount = async () => {
    try {
      await apiDelete('/api/user');
      clearSession();
      window.location.href = '/';
    } catch (err: any) {
      setError(err.message);
    }
  };

  const handleDeleteWebhook = async (id: string) => {
    try {
      setError(null);
      await apiDelete(`/api/user/webhooks/${id}`);
      setWebhooks(prev => prev.filter(w => w.id !== id));
    } catch (err: any) {
      setError(err.message);
    }
  };

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault();
    setPasswordError(null);
    setPasswordSuccess(null);

    if (!currentPassword || !newPassword) {
      setPasswordError('Current password and new password are required.');
      return;
    }
    if (newPassword.length < 10) {
      setPasswordError('New password must be at least 10 characters.');
      return;
    }
    if (newPassword.length > 64) {
      setPasswordError('New password must be 64 characters or less.');
      return;
    }

    try {
      setChangingPassword(true);
      await apiPost('/api/user/change-password', { currentPassword, newPassword });
      setCurrentPassword('');
      setNewPassword('');
      setPasswordSuccess('Password updated.');
    } catch (err: any) {
      setPasswordError(err.message || 'Failed to update password.');
    } finally {
      setChangingPassword(false);
    }
  };

  if (!user) return null;

  if (loading) {
    return <Spinner className="py-16" label="Loading settings" />;
  }

  return (
    <div className="mx-auto max-w-4xl space-y-8 px-4 py-10">
      <PageHeader
        eyebrow="// alerts & account"
        title="Settings"
        subtitle="Choose how you hear about it when Valve finally does something."
      />

      {error && <Alert tone="error">{error}</Alert>}

      {settings && (
        <Panel title="Comms">
          <div className="divide-y divide-line">
            <div className="flex items-center justify-between gap-4 pb-4">
              <div>
                <p className="font-display text-lg font-bold uppercase tracking-wider text-fg">Email alerts</p>
                <p className="text-sm text-muted">Sent to your registered email address.</p>
              </div>
              <Toggle
                label="Email alerts"
                checked={settings.emailNotificationsEnabled}
                onChange={() => handleToggleSettings('emailNotificationsEnabled')}
              />
            </div>
            <div className="flex items-center justify-between gap-4 pt-4">
              <div>
                <p className="font-display text-lg font-bold uppercase tracking-wider text-fg">Discord alerts</p>
                <p className="text-sm text-muted">Posted to every webhook configured below.</p>
              </div>
              <Toggle
                label="Discord alerts"
                checked={settings.discordNotificationsEnabled}
                onChange={() => handleToggleSettings('discordNotificationsEnabled')}
              />
            </div>
          </div>
        </Panel>
      )}

      <Panel title="Discord uplinks">
        <p className="-mt-2 mb-5 text-sm text-muted">
          Add a webhook to get a rich embed in your server's channel when a tracked account gets banned.
        </p>

        <form
          onSubmit={handleAddWebhook}
          className="grid gap-4 border border-line bg-canvas/60 p-4 sm:grid-cols-[1fr_2fr_auto] sm:items-end"
        >
          <Field
            id="webhookName"
            label="Name"
            type="text"
            required
            maxLength={100}
            value={newWebhookName}
            onChange={(e) => setNewWebhookName(e.target.value)}
            placeholder="CS2 squad #alerts"
          />
          <Field
            id="webhookUrl"
            label="Webhook URL"
            type="url"
            required
            maxLength={1000}
            value={newWebhookUrl}
            onChange={(e) => setNewWebhookUrl(e.target.value)}
            placeholder="https://discord.com/api/webhooks/..."
          />
          <Button type="submit" disabled={addingWebhook || !newWebhookName || !newWebhookUrl}>
            {addingWebhook ? 'Linking...' : 'Add'}
          </Button>
        </form>

        <div className="mt-6 space-y-3">
          {webhooks.length === 0 ? (
            <p className="py-4 text-center font-mono text-sm text-dim">No uplinks configured.</p>
          ) : (
            webhooks.map((webhook) => (
              <div
                key={webhook.id}
                className="flex flex-col gap-3 border border-line bg-canvas/60 p-4 sm:flex-row sm:items-center sm:justify-between"
              >
                <div className="min-w-0">
                  <p className="flex items-center gap-2 font-medium text-fg">
                    <span className="h-1.5 w-1.5 rounded-full bg-clean shadow-glow-clean" aria-hidden="true" />
                    {webhook.name}
                  </p>
                  <p className="max-w-[300px] truncate font-mono text-xs text-dim sm:max-w-md">{webhook.webhookUrl}</p>
                </div>
                <ConfirmButton size="sm" confirmLabel="Delete" onConfirm={() => handleDeleteWebhook(webhook.id)}>
                  Remove
                </ConfirmButton>
              </div>
            ))
          )}
        </div>
      </Panel>

      <Panel title="Security">
        <p className="-mt-2 mb-5 text-sm text-muted">
          Rotate your operator password. Your active session stays signed in.
        </p>

        {passwordError && <Alert tone="error" className="mb-4">{passwordError}</Alert>}
        {passwordSuccess && <Alert tone="success" className="mb-4">{passwordSuccess}</Alert>}

        <form
          onSubmit={handleChangePassword}
          className="grid gap-4 border border-line bg-canvas/60 p-4 sm:grid-cols-[1fr_1fr_auto] sm:items-end"
        >
          <Field
            id="current-password"
            name="current-password"
            label="Current password"
            type="password"
            autoComplete="current-password"
            enterKeyHint="next"
            required
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.target.value)}
          />
          <Field
            id="new-password"
            name="new-password"
            label="New password"
            type="password"
            autoComplete="new-password"
            enterKeyHint="done"
            minLength={10}
            maxLength={64}
            required
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            placeholder="10–64 characters"
          />
          <Button type="submit" disabled={changingPassword || !currentPassword || !newPassword}>
            {changingPassword ? 'Updating...' : 'Update password'}
          </Button>
        </form>
      </Panel>

      <Panel tone="danger" title="Danger zone">
        <p className="mb-5 text-sm text-muted">
          Permanently delete your account and everything you've tracked. There's no undo.
        </p>
        <ConfirmButton
          prompt="This wipes your account and all tracked data. Sure?"
          confirmLabel="Yes, delete it"
          onConfirm={handleDeleteAccount}
        >
          Delete account
        </ConfirmButton>
      </Panel>
    </div>
  );
}

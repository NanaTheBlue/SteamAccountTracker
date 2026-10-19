import * as React from 'react';
import { useState } from 'react';
import { Link, useSearchParams } from 'react-router';
import { apiPost, ApiError } from '../api/client';
import { AuthShell } from '../components/AuthShell';
import { Alert } from '../components/ui/Alert';
import { Button } from '../components/ui/Button';
import { Field } from '../components/ui/Field';

export function ResetPassword() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') ?? '';

  const [newPassword, setNewPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!token) {
      setError('Missing password reset token. Please request a new reset link.');
      return;
    }
    if (!newPassword || newPassword.length < 10) {
      setError('Password must be at least 10 characters.');
      return;
    }
    if (newPassword.length > 64) {
      setError('Password must be 64 characters or less.');
      return;
    }

    setIsLoading(true);
    try {
      await apiPost('/api/user/reset-password', { token, newPassword });
      setIsSuccess(true);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('An unexpected error occurred.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <AuthShell
      eyebrow="// account recovery"
      title="Set new password"
      footerPrompt="Back to"
      footerLinkTo="/login"
      footerLinkLabel="Sign in"
    >
      {isSuccess ? (
        <div className="space-y-6">
          <Alert tone="success">
            Your password has been updated. All previous sessions have been revoked.
          </Alert>
          <Link to="/login" className="btn btn-primary w-full justify-center">
            Proceed to sign in
          </Link>
        </div>
      ) : (
        <form className="space-y-6" onSubmit={handleSubmit}>
          {!token && (
            <Alert tone="error">
              No reset token found in the URL.{' '}
              <Link to="/forgot-password" className="underline">
                Request a new link
              </Link>
              .
            </Alert>
          )}
          {error && <Alert tone="error">{error}</Alert>}

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
            hint="10–64 characters"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
          />

          <Button type="submit" disabled={isLoading || !token} className="w-full">
            {isLoading ? 'Updating...' : 'Save new password'}
          </Button>
        </form>
      )}
    </AuthShell>
  );
}


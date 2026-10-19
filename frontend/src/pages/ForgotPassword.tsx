import * as React from 'react';
import { useState } from 'react';
import { Link } from 'react-router';
import { apiPost, ApiError } from '../api/client';
import { AuthShell } from '../components/AuthShell';
import { Alert } from '../components/ui/Alert';
import { Button } from '../components/ui/Button';
import { Field } from '../components/ui/Field';

export function ForgotPassword() {
  const [email, setEmail] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [submittedMessage, setSubmittedMessage] = useState<string | null>(null);
  const [devResetToken, setDevResetToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSubmittedMessage(null);

    if (!email || !/\S+@\S+\.\S+/.test(email)) {
      setError('Please enter a valid email address.');
      return;
    }

    setIsLoading(true);
    try {
      const res = await apiPost<{ message: string; devResetToken?: string }>('/api/user/forgot-password', { email });
      setSubmittedMessage(
        res?.message || 'If an account with that email exists, a password reset link has been sent.',
      );
      if (import.meta.env.DEV && res?.devResetToken) {
        setDevResetToken(res.devResetToken);
      }
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 429) {
          setError('Too many requests. Please wait a minute and try again.');
        } else {
          setError(err.message);
        }
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
      title="Reset password"
      footerPrompt="Remembered your password?"
      footerLinkTo="/login"
      footerLinkLabel="Sign in"
    >
      <form className="space-y-6" onSubmit={handleSubmit}>
        <p className="text-sm text-muted">
          Enter the email linked to your operator account and we'll dispatch a one-hour reset link.
        </p>

        {error && <Alert tone="error">{error}</Alert>}
        {submittedMessage && (
          <Alert tone="success">
            <p>{submittedMessage}</p>
            {import.meta.env.DEV && devResetToken && (
              <p className="mt-2 font-mono text-xs">
                [DEV MOCK] <Link to={`/reset-password?token=${encodeURIComponent(devResetToken)}`} className="underline">Open reset link →</Link>
              </p>
            )}
          </Alert>
        )}

        <Field
          id="email"
          name="email"
          label="Email"
          type="email"
          autoComplete="username"
          enterKeyHint="done"
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
        />

        <Button type="submit" disabled={isLoading} className="w-full">
          {isLoading ? 'Dispatching...' : 'Send reset link'}
        </Button>
      </form>
    </AuthShell>
  );
}


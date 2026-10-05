import * as React from 'react';
import { lazy, Suspense, useState } from 'react';
import { useNavigate } from 'react-router';
import { useAuth } from '../context/AuthContext';
import { ApiError } from '../api/client';
import { AuthShell } from '../components/AuthShell';
import { Alert } from '../components/ui/Alert';
import { Button } from '../components/ui/Button';
import { Field } from '../components/ui/Field';

// Local dev only: lazy + DEV guard keeps it out of production bundles.
const DevLoginPanel = import.meta.env.DEV
  ? lazy(() => import('../dev/DevTools').then(m => ({ default: m.DevLoginPanel })))
  : null;

export function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    // Validation
    if (!email || !/\S+@\S+\.\S+/.test(email)) {
      setError('Please enter a valid email address.');
      return;
    }
    if (!password || password.length < 10) {
      setError('Password must be at least 10 characters.');
      return;
    }

    setIsLoading(true);
    try {
      await login(email, password);
      navigate('/');
    } catch (err) {
      if (err instanceof ApiError) {
        if (err.status === 429) {
          setError('Too many login attempts. Please try again later.');
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
      eyebrow="// authentication required"
      title="Operator sign in"
      footerPrompt="New here?"
      footerLinkTo="/register"
      footerLinkLabel="Enlist"
    >
      <form className="space-y-6" onSubmit={handleSubmit}>
        {error && <Alert tone="error">{error}</Alert>}

        <Field
          id="email"
          label="Email"
          type="email"
          autoComplete="email"
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
        />

        <Field
          id="password"
          label="Password"
          type="password"
          autoComplete="current-password"
          required
          value={password}
          onChange={(e) => setPassword(e.target.value)}
        />

        <Button type="submit" disabled={isLoading} className="w-full">
          {isLoading ? 'Authenticating...' : 'Sign in'}
        </Button>
      </form>

      {DevLoginPanel && (
        <Suspense fallback={null}>
          <DevLoginPanel />
        </Suspense>
      )}
    </AuthShell>
  );
}

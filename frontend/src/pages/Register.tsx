import * as React from 'react';
import { useState } from 'react';
import { useNavigate } from 'react-router';
import { useAuth } from '../context/AuthContext';
import { ApiError } from '../api/client';
import { AuthShell } from '../components/AuthShell';
import { Alert } from '../components/ui/Alert';
import { Button } from '../components/ui/Button';
import { Field } from '../components/ui/Field';

export function Register() {
  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  
  const { register } = useAuth();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (username.length > 50) {
      setError('Username cannot exceed 50 characters.');
      return;
    }
    if (!email || !/\S+@\S+\.\S+/.test(email)) {
      setError('Please enter a valid email address.');
      return;
    }
    if (!password || password.length < 10) {
      setError('Password must be at least 10 characters.');
      return;
    }
    if (password.length > 64) {
      setError('Password must be 64 characters or less.');
      return;
    }

    setIsLoading(true);
    try {
      await register(username, email, password);
      // On success, redirect directly to dashboard since they are now auto-logged in
      navigate('/dashboard');
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('An unexpected error occurred during registration.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <AuthShell
      eyebrow="// new operator"
      title="Enlist"
      footerPrompt="Already enlisted?"
      footerLinkTo="/login"
      footerLinkLabel="Sign in"
    >
      <form className="space-y-6" onSubmit={handleSubmit}>
        {error && <Alert tone="error">{error}</Alert>}

        <Field
          id="username"
          name="username"
          label="Callsign"
          type="text"
          autoComplete="username"
          enterKeyHint="next"
          required
          maxLength={50}
          value={username}
          onChange={(e) => setUsername(e.target.value)}
        />

        <Field
          id="email"
          name="email"
          label="Email"
          type="email"
          autoComplete="email"
          enterKeyHint="next"
          required
          maxLength={255}
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          hint="Ban alerts get sent here."
        />

        <Field
          id="new-password"
          name="new-password"
          label="Password"
          type="password"
          autoComplete="new-password"
          enterKeyHint="done"
          required
          minLength={10}
          maxLength={64}
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          hint="10-64 characters. Don't reuse your Steam password."
        />

        <Button type="submit" disabled={isLoading} className="w-full">
          {isLoading ? 'Enlisting...' : 'Create account'}
        </Button>
      </form>
    </AuthShell>
  );
}

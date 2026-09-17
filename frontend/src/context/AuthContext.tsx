import * as React from 'react';
import { createContext, useContext, useState, useEffect } from 'react';
import { apiPost } from '../api/client';

export interface User {
  id: string;
  username: string;
  email: string;
}

interface AuthContextType {
  user: User | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (username: string, email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const storedUser = localStorage.getItem('user');
    if (storedUser) {
      try {
        setUser(JSON.parse(storedUser));
      } catch (e) {
        localStorage.removeItem('user');
      }
    }
    setLoading(false);
  }, []);

  const login = async (email: string, password: string) => {
    const data = await apiPost<{ message: string; user: User }>('/api/user/login', { email, password });
    setUser(data.user);
    localStorage.setItem('user', JSON.stringify(data.user));
  };

  const register = async (username: string, email: string, password: string) => {
    await apiPost('/api/user/register', { username, email, password });
  };

  const logout = async () => {
    try {
      await apiPost('/api/user/logout');
    } catch (e) {
      console.error('Logout failed', e);
    } finally {
      setUser(null);
      localStorage.removeItem('user');
    }
  };

  return (
    <AuthContext.Provider value={{ user, loading, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}

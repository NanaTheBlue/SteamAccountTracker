import * as React from 'react';
import { lazy, Suspense } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router';
import { AuthProvider, useAuth } from './context/AuthContext';
import { Navbar } from './components/Navbar';
import { ProtectedRoute } from './components/ProtectedRoute';
import { Landing } from './pages/Landing';
import { Dashboard } from './pages/Dashboard';
import { TrackAccount } from './pages/TrackAccount';
import { Settings } from './pages/Settings';
import { Login } from './pages/Login';
import { Register } from './pages/Register';
import { NotFound } from './pages/NotFound';

// Local dev only: lazy + DEV guard keeps it out of production bundles.
const DevMockBadge = import.meta.env.DEV
  ? lazy(() => import('./dev/DevTools').then(m => ({ default: m.DevMockBadge })))
  : null;

function PublicOnlyRoute({ children }: { children: React.ReactNode }) {
  const { user, loading } = useAuth();
  
  if (loading) return null;
  if (user) return <Navigate to="/dashboard" replace />;
  
  return <>{children}</>;
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <div className="min-h-screen flex flex-col">
          <Navbar />
          <main className="flex-grow">
            <Routes>
              {/* Public route for everyone, but redirects to dashboard if logged in */}
              <Route path="/" element={<Landing />} />
              
              {/* Public only (redirect to /dashboard if logged in) */}
              <Route path="/login" element={<PublicOnlyRoute><Login /></PublicOnlyRoute>} />
              <Route path="/register" element={<PublicOnlyRoute><Register /></PublicOnlyRoute>} />

              {/* Protected routes */}
              <Route element={<ProtectedRoute />}>
                <Route path="/dashboard" element={<Dashboard />} />
                <Route path="/track" element={<TrackAccount />} />
                <Route path="/settings" element={<Settings />} />
              </Route>

              {/* 404 */}
              <Route path="*" element={<NotFound />} />
            </Routes>
          </main>
          <footer className="border-t border-line py-6 text-center mono-label">
            CheaterWatch · reads public Steam data only · not affiliated with Valve
          </footer>
          {DevMockBadge && (
            <Suspense fallback={null}>
              <DevMockBadge />
            </Suspense>
          )}
        </div>
      </BrowserRouter>
    </AuthProvider>
  );
}

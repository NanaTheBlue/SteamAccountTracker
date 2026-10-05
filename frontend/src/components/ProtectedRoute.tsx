import { Navigate, Outlet } from 'react-router';
import { useAuth } from '../context/AuthContext';
import { Spinner } from './ui/Spinner';

export function ProtectedRoute() {
  const { user, loading } = useAuth();

  if (loading) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <Spinner label="Authenticating" />
      </div>
    );
  }

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  return <Outlet />;
}

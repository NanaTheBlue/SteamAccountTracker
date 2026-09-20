import { Link, Navigate } from 'react-router';
import { useAuth } from '../context/AuthContext';

export function Landing() {
  const { user, loading } = useAuth();

  // If auth state is still initializing, don't flash the landing page UI
  if (loading) {
    return null;
  }

  // If already logged in, skip the landing page and go straight to the dashboard
  if (user) {
    return <Navigate to="/dashboard" replace />;
  }

  return (
    <div className="flex flex-col items-center justify-center min-h-[calc(100vh-4rem)] bg-gray-950 text-white px-4">
      <div className="max-w-3xl text-center space-y-8">
        <h1 className="text-5xl md:text-6xl font-extrabold tracking-tight">
          Track Hackers. <br />
          <span className="text-blue-500">Get Notified Instantly.</span>
        </h1>
        
        <p className="text-xl text-gray-400 max-w-2xl mx-auto">
          CheaterWatch monitors suspicious Steam accounts 24/7. The moment they receive a VAC or Game Ban, we send you an email.
        </p>

        <div className="flex flex-col sm:flex-row items-center justify-center gap-4 pt-8">
          <Link 
            to="/register" 
            className="px-8 py-4 bg-blue-600 hover:bg-blue-700 text-white font-bold rounded-lg text-lg transition-colors w-full sm:w-auto"
          >
            Start Tracking for Free
          </Link>
          <Link 
            to="/login" 
            className="px-8 py-4 bg-gray-800 hover:bg-gray-700 text-white font-bold rounded-lg text-lg transition-colors border border-gray-700 w-full sm:w-auto"
          >
            Sign In
          </Link>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-8 pt-16 text-left">
          <div className="bg-gray-900 p-6 rounded-xl border border-gray-800">
            <div className="text-blue-500 text-3xl mb-4">🎯</div>
            <h3 className="text-xl font-bold mb-2">Automated Scanning</h3>
            <p className="text-gray-400">We poll the Steam API every 15 minutes to catch status changes immediately.</p>
          </div>
          <div className="bg-gray-900 p-6 rounded-xl border border-gray-800">
            <div className="text-blue-500 text-3xl mb-4">⚡</div>
            <h3 className="text-xl font-bold mb-2">Instant Alerts</h3>
            <p className="text-gray-400">Receive an email notification the exact moment a tracked account gets banned.</p>
          </div>
          <div className="bg-gray-900 p-6 rounded-xl border border-gray-800">
            <div className="text-blue-500 text-3xl mb-4">🛡️</div>
            <h3 className="text-xl font-bold mb-2">Clean UI</h3>
            <p className="text-gray-400">Easily organize and view all your suspected cheaters in one dashboard.</p>
          </div>
        </div>
      </div>
    </div>
  );
}

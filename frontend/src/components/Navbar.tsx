import { NavLink, Link, useNavigate } from 'react-router';
import { useAuth } from '../context/AuthContext';
import { Button } from './ui/Button';

const tabClass = ({ isActive }: { isActive: boolean }) =>
  `relative px-1.5 sm:px-3 py-5 font-display text-xs sm:text-sm font-bold uppercase tracking-[0.08em] sm:tracking-[0.14em] transition-colors ${
    isActive
      ? 'text-primary after:absolute after:inset-x-2 after:bottom-0 after:h-0.5 after:bg-primary after:shadow-glow-primary'
      : 'text-muted hover:text-fg'
  }`;

/** Slanted amber divider between nav tabs. */
function TabDivider() {
  return <span className="mx-0.5 inline-block h-6 w-0.5 rotate-[22deg] bg-primary sm:mx-1" aria-hidden="true" />;
}

export function Navbar() {
  const { user, logout, loading } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <nav className="sticky top-0 z-40 border-b border-line bg-canvas/85 backdrop-blur">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex h-16 items-center justify-between gap-4">
          <Link to={user ? '/dashboard' : '/'} aria-label="CheaterWatch home" className="group flex flex-shrink-0 items-center gap-2">
            <svg width="22" height="22" viewBox="0 0 24 24" fill="none" className="text-primary" aria-hidden="true">
              <circle cx="12" cy="12" r="8" stroke="currentColor" strokeWidth="2" />
              <path d="M12 1v6M12 17v6M1 12h6M17 12h6" stroke="currentColor" strokeWidth="2" />
            </svg>
            <span
              className="glitch hidden sm:inline-block font-display text-xl font-bold uppercase tracking-[0.12em] text-fg"
              data-text="Cheater/Watch"
            >
              Cheater<span className="text-primary">/</span>Watch
            </span>
          </Link>

          <div className="flex items-center gap-1 sm:gap-2">
            {loading ? (
              <div className="flex gap-3 animate-pulse">
                <div className="hidden h-8 w-20 bg-surface-2 sm:block" />
                <div className="h-8 w-24 bg-surface-2" />
              </div>
            ) : user ? (
              <>
                <NavLink to="/dashboard" className={tabClass}>
                  Suspects
                </NavLink>
                <TabDivider />
                <NavLink to="/track" className={tabClass}>
                  Flag<span className="hidden sm:inline"> a suspect</span>
                </NavLink>
                <TabDivider />
                <NavLink to="/settings" className={tabClass}>
                  Settings
                </NavLink>
                <span
                  data-testid="nav-operator"
                  className="ml-2 hidden items-center gap-2 border-l border-line pl-4 font-mono text-xs text-muted md:inline-flex"
                >
                  <span className="h-1.5 w-1.5 rounded-full bg-clean shadow-glow-clean" aria-hidden="true" />
                  operator: <span className="text-fg">{user.username}</span>
                </span>
                <Button variant="ghost" size="sm" className="ml-1 sm:ml-2" onClick={handleLogout}>
                  Log out
                </Button>
              </>
            ) : (
              <>
                <NavLink to="/login" className={tabClass}>
                  Log in
                </NavLink>
                <Button to="/register" size="sm" className="ml-2">
                  Enlist
                </Button>
              </>
            )}
          </div>
        </div>
      </div>
    </nav>
  );
}

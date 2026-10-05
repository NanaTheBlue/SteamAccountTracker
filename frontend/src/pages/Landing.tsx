import { Navigate } from 'react-router';
import { useAuth } from '../context/AuthContext';
import { Button } from '../components/ui/Button';
import { Panel } from '../components/ui/Panel';
import { AppPreview } from '../components/AppPreview';

const steps = [
  {
    tag: '01',
    title: 'You flag them',
    body: 'Paste a SteamID64, profile URL, vanity URL or STEAM_X:Y:Z of the guy who prefired every corner.',
  },
  {
    tag: '02',
    title: 'We keep checking',
    body: 'Their public ban status gets re-checked on a 15-minute cycle through the official Steam Web API.',
  },
  {
    tag: '03',
    title: 'You find out',
    body: 'When a VAC, game or community ban shows up, you get an email and/or a Discord webhook alert.',
  },
];

const does = [
  'Reads public ban data from the Steam Web API',
  'Alerts you when a ban appears on an account you flagged',
  'Shows how many other players are watching the same account',
];

const doesNot = [
  'Detect cheats, file reports or influence Valve in any way',
  'Make anyone get banned faster (sadly)',
  'Hack, dox or harass anyone. Don\'t use it to, either.',
];

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
    <div className="mx-auto max-w-6xl px-4 py-16 sm:py-24">
      {/* Hero */}
      <section className="grid items-center gap-12 lg:grid-cols-[1.15fr_1fr]">
        <div>
          <p className="mono-label mb-5">// steam ban notifications · white hat · free</p>
          <h1 className="text-5xl leading-[0.95] sm:text-6xl lg:text-7xl">
            We can't ban cheaters.
            <span className="mt-2 block text-primary">But we'll tell you when Valve does.</span>
          </h1>
          <p className="mt-6 max-w-xl text-lg text-muted">
            Flag the spinbotter who ruined your match. CheaterWatch keeps checking their public Steam ban status and
            pings you once a VAC or game ban lands. It won't get anyone banned faster. It just means you'll actually{' '}
            <span className="text-fg">find out</span>.
          </p>
          <div className="mt-10 flex flex-col gap-4 sm:flex-row">
            <Button to="/register" size="lg">
              Enlist for free
            </Button>
            <Button to="/login" variant="ghost" size="lg">
              Log in
            </Button>
          </div>
        </div>

        <AppPreview />
      </section>

      {/* How it works */}
      <section className="mt-24">
        <h2 className="hud-title mb-8">How it works</h2>
        <div className="grid gap-6 md:grid-cols-3">
          {steps.map((s) => (
            <Panel key={s.tag}>
              <p className="font-mono text-xs text-primary">STEP_{s.tag}</p>
              <h3 className="mt-2 text-2xl text-fg">{s.title}</h3>
              <p className="mt-3 text-muted">{s.body}</p>
            </Panel>
          ))}
        </div>
      </section>

      {/* Rules of engagement */}
      <section className="mt-24">
        <h2 className="hud-title mb-8">Rules of engagement</h2>
        <div className="grid gap-6 md:grid-cols-2">
          <Panel tone="clean">
            <h3 className="mb-4 text-xl text-clean">What this does</h3>
            <ul className="space-y-3 font-mono text-sm">
              {does.map((item) => (
                <li key={item} className="flex gap-3">
                  <span className="text-clean">[+]</span>
                  <span className="text-fg">{item}</span>
                </li>
              ))}
            </ul>
          </Panel>
          <Panel tone="danger">
            <h3 className="mb-4 text-xl text-danger">What this doesn't do</h3>
            <ul className="space-y-3 font-mono text-sm">
              {doesNot.map((item) => (
                <li key={item} className="flex gap-3">
                  <span className="text-danger">[-]</span>
                  <span className="text-fg">{item}</span>
                </li>
              ))}
            </ul>
          </Panel>
        </div>
        <p className="mt-6 text-center font-mono text-sm text-muted">
          Think someone's cheating? <span className="text-primary">Report them in-game.</span> That's how it gets to
          Valve. We just watch the scoreboard.
        </p>
      </section>
    </div>
  );
}

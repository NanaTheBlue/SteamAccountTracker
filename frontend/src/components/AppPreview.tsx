import { Badge } from './ui/Badge';
import { Panel } from './ui/Panel';

const sample = [
  { name: 'xX_Sp1nB0t_Xx', watchers: 4, banned: false },
  { name: 'definitely_legit', watchers: 2, banned: false },
  { name: 'wallhack_willy', watchers: 17, banned: true },
  { name: '1tap_andy', watchers: 1, banned: false },
];

/**
 * Decorative, Windows-style app window previewing the watchlist and a ban
 * alert. Purely illustrative: sample data, nothing to install or run.
 */
export function AppPreview() {
  return (
    <figure>
      <Panel className="p-0!" aria-hidden="true">
        {/* Title bar */}
        <div className="flex h-9 items-center bg-surface-2 font-sans text-xs text-muted">
          <div className="flex min-w-0 items-center gap-2 pl-3">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" className="flex-shrink-0 text-primary">
              <circle cx="12" cy="12" r="8" stroke="currentColor" strokeWidth="2.5" />
              <path d="M12 1v6M12 17v6M1 12h6M17 12h6" stroke="currentColor" strokeWidth="2.5" />
            </svg>
            <span className="truncate">CheaterWatch</span>
          </div>
          <div className="ml-auto flex self-stretch">
            <span className="flex w-10 items-center justify-center">─</span>
            <span className="flex w-10 items-center justify-center">▢</span>
            <span className="flex w-10 items-center justify-center">✕</span>
          </div>
        </div>

        <div className="bg-canvas p-5">
          {/* Header + polling status */}
          <div className="flex items-center justify-between gap-3">
            <p className="font-display text-lg font-bold uppercase tracking-wider text-fg">Your watchlist</p>
            <span className="inline-flex items-center gap-2 text-xs text-clean">
              <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-clean" />
              Checking
            </span>
          </div>
          <div className="mt-3 h-0.5 overflow-hidden bg-line">
            <div className="h-full w-1/3 animate-sweep bg-primary" />
          </div>
          <p className="mt-2 text-xs text-dim">Checked every 15 minutes · next check in 12:43</p>

          {/* Watchlist */}
          <ul className="mt-4 divide-y divide-line border border-line">
            {sample.map(s => (
              <li key={s.name} className={`flex items-center gap-3 px-3 py-2.5 ${s.banned ? 'bg-danger/5' : ''}`}>
                <span
                  className={`flex h-8 w-8 flex-shrink-0 items-center justify-center border font-display text-sm font-bold uppercase ${
                    s.banned ? 'border-danger/60 text-danger' : 'border-line-strong text-muted'
                  }`}
                >
                  {s.name.replace(/[^a-z0-9]/gi, '').charAt(0)}
                </span>
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm font-medium text-fg">{s.name}</p>
                  <p className="text-[11px] text-dim">{s.watchers} watching</p>
                </div>
                {s.banned ? <Badge tone="danger">VAC banned</Badge> : <Badge tone="clean">No bans</Badge>}
              </li>
            ))}
          </ul>

          {/* Notification toast */}
          <div className="mt-4 flex gap-3 border border-line-strong border-l-primary bg-surface-2 p-3 [border-left-width:3px]">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" className="mt-0.5 flex-shrink-0 text-primary">
              <path
                d="M6 8a6 6 0 1 1 12 0c0 7 3 9 3 9H3s3-2 3-9M10.3 21a1.94 1.94 0 0 0 3.4 0"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </svg>
            <div className="min-w-0">
              <p className="text-sm font-semibold text-fg">Ban detected: wallhack_willy</p>
              <p className="text-xs text-muted">VAC ban on record. Emailed you and posted to #cs2-alerts.</p>
              <p className="mt-1 text-[11px] text-dim">just now</p>
            </div>
          </div>
        </div>
      </Panel>
      <figcaption className="mono-label mt-3 text-right">Preview · sample data</figcaption>
    </figure>
  );
}

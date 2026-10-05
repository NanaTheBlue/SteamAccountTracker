import { Badge } from './ui/Badge';
import { ConfirmButton } from './ui/ConfirmButton';

export interface TrackedAccountDto {
  steamId64: string;
  personaName?: string;
  avatar?: string;
  avatarFull?: string;
  vacBanned: boolean;
  numberOfVACBans: number;
  numberOfGameBans: number;
  communityBanned: boolean;
  trackersCount: number;
}

export function isAccountBanned(account: TrackedAccountDto) {
  return account.vacBanned || account.communityBanned || account.numberOfVACBans + account.numberOfGameBans > 0;
}

interface AccountCardProps {
  account: TrackedAccountDto;
  onUntrack: (steamId64: string) => void;
}

export function AccountCard({ account, onUntrack }: AccountCardProps) {
  const banned = isAccountBanned(account);
  const displayName = account.personaName || account.steamId64;

  return (
    <div
      className={`panel flex h-full flex-col p-5 transition-colors ${
        banned ? 'panel-danger animate-pulse-danger' : 'hover:border-line-strong'
      }`}
    >
      <div className="mb-4">
        <div className="flex min-w-0 items-center gap-3">
          <div className={`flex-shrink-0 border p-0.5 ${banned ? 'border-danger' : 'border-line-strong'}`}>
            {account.avatarFull ? (
              <img
                src={account.avatarFull}
                alt={displayName}
                className={`h-12 w-12 ${banned ? 'grayscale-[0.6]' : ''}`}
              />
            ) : (
              <div className="flex h-12 w-12 items-center justify-center bg-surface-2">
                <span className="font-mono text-xs text-dim">??</span>
              </div>
            )}
          </div>
          <div className="min-w-0 flex-1">
            <h3 className="truncate text-xl text-fg" title={displayName}>
              {displayName}
            </h3>
            <a
              href={`https://steamcommunity.com/profiles/${account.steamId64}`}
              target="_blank"
              rel="noopener noreferrer"
              className="block truncate font-mono text-xs text-info hover:underline"
            >
              {account.steamId64} ↗
            </a>
            <span className="font-mono text-[10px] text-dim">
              {account.trackersCount} {account.trackersCount === 1 ? 'player watching' : 'players watching'}
            </span>
          </div>
        </div>

        <div className="mt-3 flex flex-wrap gap-1.5">
          {!banned && <Badge tone="clean">No bans (yet)</Badge>}
          {account.vacBanned && <Badge tone="danger">VAC banned</Badge>}
          {account.numberOfGameBans > 0 && <Badge tone="danger">Game banned</Badge>}
          {account.communityBanned && <Badge tone="danger">Community banned</Badge>}
        </div>
      </div>

      <dl className="mb-6 mt-2 grid grid-cols-2 gap-px border border-line bg-line font-mono text-xs">
        <div className="bg-surface px-3 py-2">
          <dt className="text-dim">VAC_BANS</dt>
          <dd className={account.numberOfVACBans > 0 ? 'text-lg text-danger' : 'text-lg text-fg'}>
            {account.numberOfVACBans}
          </dd>
        </div>
        <div className="bg-surface px-3 py-2">
          <dt className="text-dim">GAME_BANS</dt>
          <dd className={account.numberOfGameBans > 0 ? 'text-lg text-danger' : 'text-lg text-fg'}>
            {account.numberOfGameBans}
          </dd>
        </div>
      </dl>

      <div className="mt-auto">
        <ConfirmButton
          size="sm"
          className="w-full"
          confirmLabel="Drop"
          onConfirm={() => onUntrack(account.steamId64)}
        >
          Drop target
        </ConfirmButton>
      </div>
    </div>
  );
}

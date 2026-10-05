/**
 * DEV-ONLY in-memory mock of the CheaterWatch API.
 *
 * Only ever loaded through `import.meta.env.DEV` guards, so it is stripped
 * from production builds. Mock mode turns on when you use the dev login
 * button, or automatically when the real backend can't be reached.
 * State lives in localStorage so it survives reloads.
 */
import type { TrackedAccountDto } from '../components/AccountCard';

const MODE_KEY = 'cw-dev-mock';
const DB_KEY = 'cw-dev-mock-db';
export const MOCK_CHANGE_EVENT = 'cw-dev-mock-change';

export const DEV_TEST_USER = {
  username: 'TestOperator',
  email: 'operator@cheaterwatch.local',
  password: 'devpassword123',
};

interface Webhook {
  id: string;
  name: string;
  webhookUrl: string;
  createdAt: string;
}

interface MockDb {
  accounts: TrackedAccountDto[];
  settings: { emailNotificationsEnabled: boolean; discordNotificationsEnabled: boolean };
  webhooks: Webhook[];
}

// --- mode -------------------------------------------------------------------

export function isMockMode() {
  try {
    return localStorage.getItem(MODE_KEY) === '1';
  } catch {
    return false;
  }
}

export function setMockMode(on: boolean) {
  try {
    if (on) localStorage.setItem(MODE_KEY, '1');
    else localStorage.removeItem(MODE_KEY);
  } catch {
    /* storage unavailable: mock mode just won't persist */
  }
  window.dispatchEvent(new Event(MOCK_CHANGE_EVENT));
}

// --- data -------------------------------------------------------------------

/** Simple generated avatar so cards don't all show "??". */
function avatar(seed: string) {
  let h = 0;
  for (const c of seed) h = (h * 31 + c.charCodeAt(0)) >>> 0;
  const hue = h % 360;
  const cells = Array.from({ length: 25 }, (_, i) => ((h >> i % 24) & 1 ? i : -1))
    .filter(i => i >= 0 && i % 5 < 3)
    .flatMap(i => {
      const x = i % 5, y = Math.floor(i / 5);
      return [x, 4 - x].map(cx => `<rect x="${cx * 8 + 4}" y="${y * 8 + 4}" width="8" height="8"/>`);
    })
    .join('');
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 48 48"><rect width="48" height="48" fill="hsl(${hue},25%,14%)"/><g fill="hsl(${hue},70%,58%)">${cells}</g></svg>`;
  return `data:image/svg+xml,${encodeURIComponent(svg)}`;
}

function makeAccount(
  steamId64: string,
  personaName: string,
  bans: Partial<Pick<TrackedAccountDto, 'vacBanned' | 'numberOfVACBans' | 'numberOfGameBans' | 'communityBanned'>> = {},
  trackersCount = 1,
): TrackedAccountDto {
  const av = avatar(steamId64);
  return {
    steamId64,
    personaName,
    avatar: av,
    avatarFull: av,
    vacBanned: false,
    numberOfVACBans: 0,
    numberOfGameBans: 0,
    communityBanned: false,
    trackersCount,
    ...bans,
  };
}

function seed(): MockDb {
  return {
    accounts: [
      makeAccount('76561198012345601', 'xX_Sp1nB0t_Xx', {}, 4),
      makeAccount('76561198012345602', 'wallhack_willy', { vacBanned: true, numberOfVACBans: 1 }, 17),
      makeAccount('76561198012345603', 'definitely_legit_player_trust_me', {}, 2),
      makeAccount('76561198012345604', 'triggerbot_tim', { numberOfGameBans: 2 }, 9),
      makeAccount('76561198012345605', '1tap_andy', {}, 1),
      makeAccount('76561198012345606', 'toxic_and_banned', { vacBanned: true, numberOfVACBans: 2, numberOfGameBans: 1, communityBanned: true }, 31),
    ],
    settings: { emailNotificationsEnabled: true, discordNotificationsEnabled: true },
    webhooks: [
      {
        id: 'wh-1',
        name: 'CS2 squad #ban-alerts',
        webhookUrl: 'https://discord.com/api/webhooks/000000000000000000/mock-token-not-real',
        createdAt: new Date().toISOString(),
      },
    ],
  };
}

function load(): MockDb {
  try {
    const raw = localStorage.getItem(DB_KEY);
    if (raw) return JSON.parse(raw) as MockDb;
  } catch {
    /* fall through to a fresh seed */
  }
  return seed();
}

function save(db: MockDb) {
  try {
    localStorage.setItem(DB_KEY, JSON.stringify(db));
  } catch {
    /* ignore */
  }
}

export function resetMockDb() {
  save(seed());
}

// --- helpers ----------------------------------------------------------------

const STEAM_BASE = 76561197960265728n;

/** Mirrors the formats the real backend accepts; vanity names get a stable fake ID. */
function resolveSteamInput(input: string): { steamId64: string; name?: string } | null {
  const s = input.trim();
  let m = s.match(/^(\d{17})$/) || s.match(/steamcommunity\.com\/profiles\/(\d{17})/);
  if (m) return { steamId64: m[1]! };

  m = s.match(/^STEAM_[0-5]:([01]):(\d+)$/i);
  if (m) return { steamId64: (STEAM_BASE + BigInt(m[2]!) * 2n + BigInt(m[1]!)).toString() };

  m = s.match(/steamcommunity\.com\/id\/([\w-]{2,32})/) || s.match(/^([\w-]{2,32})$/);
  if (m) {
    let h = 0n;
    for (const c of m[1]!.toLowerCase()) h = (h * 131n + BigInt(c.charCodeAt(0))) % 4000000000n;
    return { steamId64: (STEAM_BASE + h).toString(), name: m[1] };
  }
  return null;
}

function json(body: unknown, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
}

function error(message: string, status: number) {
  return json({ message }, status);
}

const delay = (ms: number) => new Promise(r => setTimeout(r, ms));

// --- router -----------------------------------------------------------------

export async function mockFetch(path: string, init: RequestInit = {}): Promise<Response> {
  await delay(250 + Math.random() * 250); // long enough to see loading states

  const method = (init.method || 'GET').toUpperCase();
  const body = typeof init.body === 'string' ? JSON.parse(init.body) : undefined;
  const db = load();
  const route = `${method} ${path.split('?')[0]}`;
  let m: RegExpMatchArray | null;

  console.debug(`[mock api] ${route}`, body ?? '');

  switch (route) {
    case 'POST /api/user/login':
    case 'POST /api/user/register': {
      const email: string = body?.email || DEV_TEST_USER.email;
      const username: string =
        body?.username || (email === DEV_TEST_USER.email ? DEV_TEST_USER.username : email.split('@')[0]) || 'TestOperator';
      setMockMode(true);
      return json({ message: 'OK (mock)', user: { id: 'dev-user', username, email } });
    }
    case 'POST /api/user/logout':
      setMockMode(false);
      return json({ message: 'Logged out (mock).' });

    case 'DELETE /api/user':
      resetMockDb();
      setMockMode(false);
      try {
        localStorage.removeItem('user');
      } catch {
        /* ignore */
      }
      return json({ message: 'Account deleted (mock).' });

    case 'GET /api/user/settings':
      return json(db.settings);
    case 'PUT /api/user/settings':
      db.settings = { ...db.settings, ...body };
      save(db);
      return json({ message: 'Settings updated (mock).' });

    case 'GET /api/user/webhooks':
      return json(db.webhooks);
    case 'POST /api/user/webhooks': {
      if (!String(body?.webhookUrl).startsWith('https://discord.com/api/webhooks/')) {
        return error('Only Discord webhook URLs are supported (https://discord.com/api/webhooks/...).', 400);
      }
      const wh: Webhook = {
        id: `wh-${Date.now()}`,
        name: body.name,
        webhookUrl: body.webhookUrl,
        createdAt: new Date().toISOString(),
      };
      db.webhooks.unshift(wh);
      save(db);
      return json(wh);
    }

    case 'GET /api/steam/tracked':
      return json({ accounts: db.accounts });
    case 'POST /api/steam/track': {
      const resolved = resolveSteamInput(String(body?.steamInput ?? ''));
      if (!resolved) {
        return error(
          'Could not resolve the provided input to a valid Steam account. Accepted formats: SteamID64, STEAM_X:Y:Z, vanity URL, or profile URL.',
          400,
        );
      }
      if (db.accounts.some(a => a.steamId64 === resolved.steamId64)) {
        return error('You are already tracking this Steam account.', 409);
      }
      // Roughly 1 in 4 new suspects already has a ban on record.
      const banned = BigInt(resolved.steamId64) % 4n === 0n;
      db.accounts.unshift(
        makeAccount(
          resolved.steamId64,
          resolved.name ?? `suspect_${resolved.steamId64.slice(-4)}`,
          banned ? { vacBanned: true, numberOfVACBans: 1 } : {},
          1 + Number(BigInt(resolved.steamId64) % 7n),
        ),
      );
      save(db);
      return json({ message: 'Steam account is now being tracked. (mock)', steamId64: resolved.steamId64 });
    }
  }

  if ((m = route.match(/^DELETE \/api\/steam\/track\/(\d+)$/))) {
    const before = db.accounts.length;
    db.accounts = db.accounts.filter(a => a.steamId64 !== m![1]);
    if (db.accounts.length === before) return error('Account was not being tracked by this user.', 404);
    save(db);
    return json({ message: 'Account untracked successfully. (mock)' });
  }

  if ((m = route.match(/^DELETE \/api\/user\/webhooks\/(.+)$/))) {
    const before = db.webhooks.length;
    db.webhooks = db.webhooks.filter(w => w.id !== m![1]);
    if (db.webhooks.length === before) return error('Webhook not found.', 404);
    save(db);
    return json({ message: 'Webhook deleted successfully. (mock)' });
  }

  return error(`No mock for ${route}`, 404);
}

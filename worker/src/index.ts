import { withSentry } from '@sentry/cloudflare';

interface Env {
  API_BASE_URL: string;
  WORKER_KEY: string;
  STEAM_API_KEY: string;
  RESEND_API_KEY: string;
  FROM_EMAIL: string;
  SENTRY_DSN?: string;
  SCAN_QUEUE: Queue<TrackedAccount[]>;
}

interface TrackedAccount {
  steamId64: string;
  vacBanned: boolean;
  numberOfVACBans: number;
  numberOfGameBans: number;
  communityBanned: boolean;
}

interface AccountsPage {
  accounts: TrackedAccount[];
  offset: number;
  limit: number;
  count: number;
}

interface SteamBanResponse {
  players: SteamPlayerBan[];
}

interface SteamPlayerBan {
  SteamId: string;
  CommunityBanned: boolean;
  VACBanned: boolean;
  NumberOfVACBans: number;
  DaysSinceLastBan: number;
  NumberOfGameBans: number;
  EconomyBan: string;
}

interface BanUpdate {
  steamId64: string;
  vacBanned: boolean;
  numberOfVACBans: number;
  numberOfGameBans: number;
  communityBanned: boolean;
}

interface NotificationEntry {
  email: string;
  username: string;
  steamId64: string;
  banType: string;
}

export function escapeHtml(unsafe: string): string {
  return unsafe
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#039;");
}

export function chunk<T>(array: T[], size: number): T[][] {
  const result: T[][] = [];
  for (let i = 0; i < array.length; i += size) {
    result.push(array.slice(i, i + size));
  }
  return result;
}

async function apiRequest<T>(url: string, env: Env, options?: RequestInit): Promise<T> {
  const response = await fetch(url, {
    ...options,
    headers: {
      'X-Worker-Key': env.WORKER_KEY,
      'Content-Type': 'application/json',
      ...options?.headers,
    },
  });

  if (!response.ok) {
    throw new Error(`API request failed: ${response.status} ${await response.text()}`);
  }

  return response.json() as Promise<T>;
}

export default withSentry(
  (env: Env) => ({
    dsn: env.SENTRY_DSN,
  }),
  {
  async fetch(request, env, ctx) {
    return new Response(JSON.stringify({ status: 'ok', worker: 'cheaterwatch-ban-scanner' }), {
      headers: { 'Content-Type': 'application/json' },
    });
  },

  // PRODUCER: Runs on a cron schedule, grabs accounts, and puts them in the queue
  async scheduled(controller, env, ctx) {
    console.log(`[scheduled] Starting ban scan at ${new Date(controller.scheduledTime).toISOString()}`);

    try {
      const allAccounts: TrackedAccount[] = [];
      let offset = 0;
      const pageSize = 1000;

      // 1. Fetch all accounts from your API
      while (true) {
        const page = await apiRequest<AccountsPage>(
          `${env.API_BASE_URL}/api/internal/steam/accounts-to-scan?offset=${offset}&limit=${pageSize}`,
          env
        );
        allAccounts.push(...page.accounts);

        if (page.count < pageSize) break; // last page
        offset += pageSize;
      }

      console.log(`[scheduled] Fetched ${allAccounts.length} accounts to scan.`);
      if (allAccounts.length === 0) return;

      // 2. Chunk into batches of 100 (Steam API limit)
      const batches = chunk(allAccounts, 100);
      console.log(`[scheduled] Divided into ${batches.length} batches of up to 100. Pushing to queue...`);

      // 3. Send batches to the Queue (max 100 messages per sendBatch)
      const messageBatches = chunk(batches, 100);
      
      for (const batchOfMessages of messageBatches) {
         await env.SCAN_QUEUE.sendBatch(
           batchOfMessages.map(b => ({ body: b }))
         );
      }

      console.log(`[scheduled] Successfully queued ${batches.length} batches for processing.`);
    } catch (error) {
      console.error(`[scheduled] Producer failed: ${error}`);
      throw error;
    }
  },

  // CONSUMER: Automatically triggered by Cloudflare when messages arrive in the queue
  async queue(batch: MessageBatch<any>, env: Env, ctx: ExecutionContext) {
    console.log(`[queue] Processing batch of ${batch.messages.length} messages.`);

    for (const message of batch.messages) {
      const accounts: TrackedAccount[] = message.body; // Array of up to 100 TrackedAccounts
      
      try {
        const accountMap = new Map<string, TrackedAccount>();
        accounts.forEach(a => accountMap.set(a.steamId64, a));

        const steamIds = accounts.map(a => a.steamId64).join(',');
        const steamApiUrl = `https://api.steampowered.com/ISteamUser/GetPlayerBans/v1/?key=${env.STEAM_API_KEY}&steamids=${steamIds}`;

        // 1. Check Steam API
        const response = await fetch(steamApiUrl);
        
        if (response.status === 429) {
          console.warn(`[queue] Steam API rate limited. Retrying message later.`);
          message.retry(); // Automatically puts this chunk back in the queue
          continue;
        }
        
        if (!response.ok) {
          throw new Error(`Steam API error: ${response.status}`);
        }

        const data = await response.json() as SteamBanResponse;
        const updates: BanUpdate[] = [];

        // 2. Compare for bans
        for (const player of data.players) {
          const tracked = accountMap.get(player.SteamId);
          if (!tracked) continue;

          if (
            tracked.vacBanned !== player.VACBanned ||
            tracked.numberOfVACBans !== player.NumberOfVACBans ||
            tracked.numberOfGameBans !== player.NumberOfGameBans ||
            tracked.communityBanned !== player.CommunityBanned
          ) {
            updates.push({
              steamId64: player.SteamId,
              vacBanned: player.VACBanned,
              numberOfVACBans: player.NumberOfVACBans,
              numberOfGameBans: player.NumberOfGameBans,
              communityBanned: player.CommunityBanned,
            });
          }
        }

        // 3. Report changes to API and get emails
        let notifications: NotificationEntry[] = [];
        if (updates.length > 0) {
          console.log(`[queue] Found ${updates.length} ban changes. Updating database...`);
          const updateRes = await apiRequest<{ notifications: NotificationEntry[] }>(
            `${env.API_BASE_URL}/api/internal/steam/ban-updates`,
            env,
            {
              method: 'POST',
              body: JSON.stringify({ updates: updates }),
            }
          );
          notifications = updateRes.notifications;
        }

        // 4. Mark these specific accounts as scanned
        await apiRequest(
          `${env.API_BASE_URL}/api/internal/steam/mark-scanned`,
          env,
          {
            method: 'POST',
            body: JSON.stringify(accounts.map(a => a.steamId64)),
          }
        );

        // 5. Send emails
        if (notifications.length > 0) {
          console.log(`[queue] Sending ${notifications.length} email notifications...`);
          
          const results = await Promise.allSettled(
            notifications.map(notification =>
              fetch('https://api.resend.com/emails', {
                method: 'POST',
                headers: {
                  Authorization: `Bearer ${env.RESEND_API_KEY}`,
                  'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                  from: `CheaterWatch <${env.FROM_EMAIL || 'alerts@cheaterwatch.com'}>`,
                  to: [notification.email],
                  subject: `🚨 Ban Detected — Tracked Account ${notification.steamId64}`,
                  html: `
                    <p>Hi ${escapeHtml(notification.username)},</p>
                    <p>A Steam account you're tracking has received a new ban.</p>
                    <p><strong>Steam ID:</strong> ${escapeHtml(notification.steamId64)}</p>
                    <p><strong>Ban type:</strong> ${escapeHtml(notification.banType)}</p>
                    <p><a href="https://steamcommunity.com/profiles/${escapeHtml(notification.steamId64)}">View on Steam</a></p>
                    <p>— CheaterWatch</p>
                  `,
                }),
              })
            )
          );

          results.forEach((res, i) => {
            if (res.status === 'rejected' || !res.value.ok) {
              console.error(`[queue] Failed to email ${notifications[i].email}`);
            }
          });
        }

        // Tell the queue this message was successfully processed
        message.ack();
        
      } catch (error) {
        console.error(`[queue] Failed to process message: ${error}`);
        message.retry(); // Puts it back in the queue to try again
      }
    }
  },
});

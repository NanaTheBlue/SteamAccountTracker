interface Env {
  API_BASE_URL: string;
  WORKER_KEY: string;
  STEAM_API_KEY: string;
  RESEND_API_KEY: string;
  FROM_EMAIL: string;
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

function chunk<T>(array: T[], size: number): T[][] {
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

function shouldStop(requestCount: number, startTime: number, maxRequests: number): boolean {
  return requestCount >= maxRequests || Date.now() - startTime > 13 * 60 * 1000;
}

export default {
  async fetch(request: Request, env: Env, ctx: ExecutionContext): Promise<Response> {
    return new Response(JSON.stringify({ status: 'ok', worker: 'cheaterwatch-ban-scanner' }), {
      headers: { 'Content-Type': 'application/json' },
    });
  },

  async scheduled(event: ScheduledEvent, env: Env, ctx: ExecutionContext): Promise<void> {
    const MAX_REQUESTS = 40;
    let requestCount = 0;
    const startTime = Date.now();

    console.log(`[scheduled] Starting ban scan at ${new Date(event.scheduledTime).toISOString()}`);

    try {
      // 1. Fetch accounts with pagination
      const allAccounts: TrackedAccount[] = [];
      let offset = 0;
      const pageSize = 1000;

      while (!shouldStop(requestCount, startTime, MAX_REQUESTS)) {
        const page = await apiRequest<AccountsPage>(
          `${env.API_BASE_URL}/api/internal/steam/accounts-to-scan?offset=${offset}&limit=${pageSize}`,
          env
        );
        requestCount++;
        allAccounts.push(...page.accounts);

        if (page.count < pageSize) break; // last page
        offset += pageSize;
      }

      console.log(`[scheduled] Fetched ${allAccounts.length} accounts to scan.`);
      if (allAccounts.length === 0) return;

      const accountMap = new Map<string, TrackedAccount>();
      allAccounts.forEach(a => accountMap.set(a.steamId64, a));

      // 2. Batch and scan against Steam API
      const batches = chunk(allAccounts, 100);
      console.log(`[scheduled] Divided into ${batches.length} batches.`);
      const allUpdates: BanUpdate[] = [];

      for (let i = 0; i < batches.length; i++) {
        if (shouldStop(requestCount, startTime, MAX_REQUESTS)) {
          console.warn(`[scheduled] Safety limit reached at batch ${i}/${batches.length}. Stopping early.`);
          break;
        }

        const batch = batches[i];
        const steamIds = batch.map(a => a.steamId64).join(',');
        const steamApiUrl = `https://api.steampowered.com/ISteamUser/GetPlayerBans/v1/?key=${env.STEAM_API_KEY}&steamids=${steamIds}`;

        const response = await fetch(steamApiUrl);
        requestCount++;

        if (response.status === 429) {
          console.warn(`[scheduled] Steam API rate limited (429). Stopping early.`);
          break;
        }
        if (!response.ok) {
          console.error(`[scheduled] Steam API error: ${response.status}`);
          continue;
        }

        const data = await response.json() as SteamBanResponse;

        for (const player of data.players) {
          const tracked = accountMap.get(player.SteamId);
          if (!tracked) continue;

          if (
            tracked.vacBanned !== player.VACBanned ||
            tracked.numberOfVACBans !== player.NumberOfVACBans ||
            tracked.numberOfGameBans !== player.NumberOfGameBans ||
            tracked.communityBanned !== player.CommunityBanned
          ) {
            allUpdates.push({
              steamId64: player.SteamId,
              vacBanned: player.VACBanned,
              numberOfVACBans: player.NumberOfVACBans,
              numberOfGameBans: player.NumberOfGameBans,
              communityBanned: player.CommunityBanned,
            });
          }
        }
      }

      console.log(`[scheduled] Detected ${allUpdates.length} ban changes.`);

      // 3. Report changes to API
      let notifications: NotificationEntry[] = [];
      if (allUpdates.length > 0) {
        if (shouldStop(requestCount, startTime, MAX_REQUESTS)) {
          console.warn(`[scheduled] Safety limit reached before reporting updates. Will retry next run.`);
          return;
        }

        const updateRes = await apiRequest<{ notifications: NotificationEntry[] }>(
          `${env.API_BASE_URL}/api/internal/steam/ban-updates`,
          env,
          {
            method: 'POST',
            body: JSON.stringify({ updates: allUpdates }),
          }
        );
        requestCount++;
        notifications = updateRes.notifications;
      }

      console.log(`[scheduled] Sending ${notifications.length} email notifications.`);

      // 4. Send emails in parallel batches of 10
      let emailsSent = 0;
      const emailBatches = chunk(notifications, 10);

      for (const emailBatch of emailBatches) {
        if (shouldStop(requestCount, startTime, MAX_REQUESTS)) {
          console.warn(`[scheduled] Safety limit reached during email sending. ${emailsSent} sent so far.`);
          break;
        }

        const results = await Promise.allSettled(
          emailBatch.map(notification =>
            fetch('https://api.resend.com/emails', {
              method: 'POST',
              headers: {
                Authorization: `Bearer ${env.RESEND_API_KEY}`,
                'Content-Type': 'application/json',
              },
              body: JSON.stringify({
                from: `CheaterWatch <${env.FROM_EMAIL || 'alerts@strykz.net'}>`,
                to: [notification.email],
                subject: `🚨 Ban Detected — Tracked Account ${notification.steamId64}`,
                html: `
                  <p>Hi ${notification.username},</p>
                  <p>A Steam account you're tracking has received a new ban.</p>
                  <p><strong>Steam ID:</strong> ${notification.steamId64}</p>
                  <p><strong>Ban type:</strong> ${notification.banType}</p>
                  <p><a href="https://steamcommunity.com/profiles/${notification.steamId64}">View on Steam</a></p>
                  <p>— CheaterWatch</p>
                `,
              }),
            })
          )
        );

        requestCount += emailBatch.length;

        for (let i = 0; i < results.length; i++) {
          const result = results[i];
          if (result.status === 'fulfilled' && result.value.ok) {
            emailsSent++;
          } else {
            const reason = result.status === 'rejected' ? result.reason : `HTTP ${(result.value as Response).status}`;
            console.error(`[scheduled] Failed to email ${emailBatch[i].email}: ${reason}`);
          }
        }
      }

      console.log(`[scheduled] Done. ${emailsSent}/${notifications.length} emails sent. ${requestCount} total API requests.`);
    } catch (error) {
      console.error(`[scheduled] Worker failed: ${error}`);
    }
  },
};

using Microsoft.Data.SqlClient;
using System.Data;
using WebApplication1.Dtos;
using WebApplication1.Models;

namespace WebApplication1.Repository
{
    public class SteamRepository : ISteamRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<SteamRepository> _logger;

        public SteamRepository(IConfiguration config, ILogger<SteamRepository> logger)
        {
            _connectionString = config.GetConnectionString("CONNECTION_STRING")
                ?? config["CONNECTION_STRING"]
                ?? throw new InvalidOperationException("Connection string 'CONNECTION_STRING' not found.");
            _logger = logger;
        }

        public async Task<bool> TrackSteamAccount(string userId, string steamId64)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var transaction = conn.BeginTransaction();
            try
            {
                Guid steamAccountId;

                using (var getCmd = new SqlCommand("SELECT Id FROM SteamAccounts WHERE SteamId64 = @steamId64;", conn, transaction))
                {
                    getCmd.Parameters.Add("@steamId64", SqlDbType.NVarChar, 17).Value = steamId64;
                    var result = await getCmd.ExecuteScalarAsync();

                    if (result != null)
                    {
                        steamAccountId = (Guid)result;
                    }
                    else
                    {
                        using var insertCmd = new SqlCommand(@"
                            INSERT INTO SteamAccounts (SteamId64)
                            OUTPUT inserted.Id
                            VALUES (@steamId64);", conn, transaction);
                        insertCmd.Parameters.Add("@steamId64", SqlDbType.NVarChar, 17).Value = steamId64;
                        steamAccountId = (Guid)(await insertCmd.ExecuteScalarAsync())!;
                    }
                }

                // Check if already tracked by this user
                using (var checkCmd = new SqlCommand("SELECT 1 FROM UserSteamAccounts WHERE UserId = @userId AND SteamAccountId = @saId", conn, transaction))
                {
                    checkCmd.Parameters.Add("@userId", SqlDbType.UniqueIdentifier).Value = Guid.Parse(userId);
                    checkCmd.Parameters.Add("@saId", SqlDbType.UniqueIdentifier).Value = steamAccountId;
                    if (await checkCmd.ExecuteScalarAsync() != null)
                    {
                        await transaction.CommitAsync();
                        return false; // Already tracked
                    }
                }

                using (var linkCmd = new SqlCommand(@"
                    INSERT INTO UserSteamAccounts (UserId, SteamAccountId)
                    VALUES (@userId, @steamAccountId);", conn, transaction))
                {
                    linkCmd.Parameters.Add("@userId", SqlDbType.UniqueIdentifier).Value = Guid.Parse(userId);
                    linkCmd.Parameters.Add("@steamAccountId", SqlDbType.UniqueIdentifier).Value = steamAccountId;
                    await linkCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (SqlException e) when (e.Number == 2627)
            {
                await transaction.RollbackAsync();
                // 2627 means a unique constraint violation.
                // Because we explicitly checked if the link existed right before inserting, 
                // hitting this means another thread just inserted it at the exact same millisecond.
                // Either way, it's tracked now.
                return false;
            }
        }

        public async Task<bool> DeleteTrackedAccount(string userId, string steamId64)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            try
            {
                using var cmd = new SqlCommand(@"
                    DELETE usa
                    FROM UserSteamAccounts usa
                    INNER JOIN SteamAccounts sa ON usa.SteamAccountId = sa.Id
                    WHERE usa.UserId = @userId AND sa.SteamId64 = @steamId64;", conn);

                cmd.Parameters.Add("@userId", SqlDbType.UniqueIdentifier).Value = Guid.Parse(userId);
                cmd.Parameters.Add("@steamId64", SqlDbType.NVarChar, 17).Value = steamId64;

                var rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (SqlException e)
            {
                _logger.LogError(e, "Failed to delete tracked Steam account {SteamId64} for user {UserId}", steamId64, userId);
                throw;
            }
        }

        public async Task<List<TrackedAccountDto>> GetTrackedAccountsByUser(string userId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            try
            {
                using var cmd = new SqlCommand(@"
                    SELECT sa.SteamId64, sa.VACBanned, sa.NumberOfVACBans, sa.NumberOfGameBans, sa.CommunityBanned,
                           (SELECT COUNT(*) FROM UserSteamAccounts WHERE SteamAccountId = sa.Id) AS TrackersCount
                    FROM SteamAccounts sa
                    INNER JOIN UserSteamAccounts usa ON sa.Id = usa.SteamAccountId
                    WHERE usa.UserId = @userId
                    ORDER BY sa.SteamId64;", conn);

                cmd.Parameters.Add("@userId", SqlDbType.UniqueIdentifier).Value = Guid.Parse(userId);

                using var reader = await cmd.ExecuteReaderAsync();
                var accounts = new List<TrackedAccountDto>();

                var steamId64Ord = reader.GetOrdinal("SteamId64");
                var vacBannedOrd = reader.GetOrdinal("VACBanned");
                var numVacOrd = reader.GetOrdinal("NumberOfVACBans");
                var numGameOrd = reader.GetOrdinal("NumberOfGameBans");
                var communityOrd = reader.GetOrdinal("CommunityBanned");
                var trackersCountOrd = reader.GetOrdinal("TrackersCount");

                while (await reader.ReadAsync())
                {
                    accounts.Add(new TrackedAccountDto
                    {
                        SteamId64 = reader.GetString(steamId64Ord),
                        VACBanned = reader.GetBoolean(vacBannedOrd),
                        NumberOfVACBans = reader.GetInt32(numVacOrd),
                        NumberOfGameBans = reader.GetInt32(numGameOrd),
                        CommunityBanned = reader.GetBoolean(communityOrd),
                        TrackersCount = reader.GetInt32(trackersCountOrd)
                    });
                }

                return accounts;
            }
            catch (SqlException e)
            {
                _logger.LogError(e, "Failed to get tracked accounts for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<TrackedAccountDto>> GetAllTrackedAccounts(int offset, int limit)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            try
            {
                using var cmd = new SqlCommand(@"
                    SELECT SteamId64, VACBanned, NumberOfVACBans, NumberOfGameBans, CommunityBanned, 0 AS TrackersCount
                    FROM SteamAccounts
                    ORDER BY LastScannedAt ASC
                    OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;", conn);

                cmd.Parameters.Add("@offset", SqlDbType.Int).Value = offset;
                cmd.Parameters.Add("@limit", SqlDbType.Int).Value = limit;

                using var reader = await cmd.ExecuteReaderAsync();
                var accounts = new List<TrackedAccountDto>();

                var steamId64Ord = reader.GetOrdinal("SteamId64");
                var vacBannedOrd = reader.GetOrdinal("VACBanned");
                var numVacOrd = reader.GetOrdinal("NumberOfVACBans");
                var numGameOrd = reader.GetOrdinal("NumberOfGameBans");
                var communityOrd = reader.GetOrdinal("CommunityBanned");
                var trackersCountOrd = reader.GetOrdinal("TrackersCount");

                while (await reader.ReadAsync())
                {
                    accounts.Add(new TrackedAccountDto
                    {
                        SteamId64 = reader.GetString(steamId64Ord),
                        VACBanned = reader.GetBoolean(vacBannedOrd),
                        NumberOfVACBans = reader.GetInt32(numVacOrd),
                        NumberOfGameBans = reader.GetInt32(numGameOrd),
                        CommunityBanned = reader.GetBoolean(communityOrd),
                        TrackersCount = reader.GetInt32(trackersCountOrd)
                    });
                }

                return accounts;
            }
            catch (SqlException e)
            {
                _logger.LogError(e, "Failed to get tracked accounts");
                throw;
            }
        }

        public async Task<List<NotificationEntry>> UpdateBanStatusAndGetNotifications(List<BanUpdateEntry> updates)
        {
            var notifications = new List<NotificationEntry>();

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            foreach (var update in updates)
            {
                using var transaction = conn.BeginTransaction();
                try
                {
                    // Determine what changed for the notification message
                    var banTypes = new List<string>();
                    using (var getCmd = new SqlCommand(@"
                        SELECT VACBanned, NumberOfVACBans, NumberOfGameBans, CommunityBanned
                        FROM SteamAccounts WHERE SteamId64 = @steamId64;", conn, transaction))
                    {
                        getCmd.Parameters.Add("@steamId64", SqlDbType.NVarChar, 17).Value = update.SteamId64;
                        using var reader = await getCmd.ExecuteReaderAsync();
                        if (await reader.ReadAsync())
                        {
                            var oldVac = reader.GetBoolean(0);
                            var oldVacCount = reader.GetInt32(1);
                            var oldGameCount = reader.GetInt32(2);
                            var oldCommunity = reader.GetBoolean(3);

                            if (!oldVac && update.VACBanned) banTypes.Add("VAC Ban");
                            else if (update.NumberOfVACBans > oldVacCount) banTypes.Add("New VAC Ban");
                            if (update.NumberOfGameBans > oldGameCount) banTypes.Add("Game Ban");
                            if (!oldCommunity && update.CommunityBanned) banTypes.Add("Community Ban");
                        }
                    }

                    var banType = banTypes.Count > 0 ? string.Join(", ", banTypes) : "Ban Update";

                    // Update ban status and LastScannedAt
                    using (var updateCmd = new SqlCommand(@"
                        UPDATE SteamAccounts
                        SET VACBanned = @vacBanned,
                            NumberOfVACBans = @numVac,
                            NumberOfGameBans = @numGame,
                            CommunityBanned = @community,
                            LastScannedAt = @scannedAt
                        WHERE SteamId64 = @steamId64;", conn, transaction))
                    {
                        updateCmd.Parameters.Add("@vacBanned", SqlDbType.Bit).Value = update.VACBanned;
                        updateCmd.Parameters.Add("@numVac", SqlDbType.Int).Value = update.NumberOfVACBans;
                        updateCmd.Parameters.Add("@numGame", SqlDbType.Int).Value = update.NumberOfGameBans;
                        updateCmd.Parameters.Add("@community", SqlDbType.Bit).Value = update.CommunityBanned;
                        updateCmd.Parameters.Add("@scannedAt", SqlDbType.DateTime2).Value = DateTime.UtcNow;
                        updateCmd.Parameters.Add("@steamId64", SqlDbType.NVarChar, 17).Value = update.SteamId64;
                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    // Get all users tracking this account
                    var userWebhooks = new Dictionary<Guid, List<string>>();
                    
                    using (var webhooksCmd = new SqlCommand(@"
                        SELECT uw.UserId, uw.WebhookUrl
                        FROM UserWebhooks uw
                        INNER JOIN UserSteamAccounts usa ON uw.UserId = usa.UserId
                        INNER JOIN SteamAccounts sa ON usa.SteamAccountId = sa.Id
                        WHERE sa.SteamId64 = @steamId64;", conn, transaction))
                    {
                        webhooksCmd.Parameters.Add("@steamId64", SqlDbType.NVarChar, 17).Value = update.SteamId64;
                        using var webhookReader = await webhooksCmd.ExecuteReaderAsync();
                        while (await webhookReader.ReadAsync())
                        {
                            var uId = webhookReader.GetGuid(0);
                            var url = webhookReader.GetString(1);
                            if (!userWebhooks.ContainsKey(uId)) userWebhooks[uId] = new List<string>();
                            userWebhooks[uId].Add(url);
                        }
                    }

                    using (var usersCmd = new SqlCommand(@"
                        SELECT u.Id, u.Email, u.Username, u.EmailNotificationsEnabled, u.DiscordNotificationsEnabled
                        FROM Users u
                        INNER JOIN UserSteamAccounts usa ON u.Id = usa.UserId
                        INNER JOIN SteamAccounts sa ON usa.SteamAccountId = sa.Id
                        WHERE sa.SteamId64 = @steamId64;", conn, transaction))
                    {
                        usersCmd.Parameters.Add("@steamId64", SqlDbType.NVarChar, 17).Value = update.SteamId64;
                        using var userReader = await usersCmd.ExecuteReaderAsync();

                        while (await userReader.ReadAsync())
                        {
                            var userId = userReader.GetGuid(0);
                            var discordEnabled = userReader.GetBoolean(4);
                            
                            notifications.Add(new NotificationEntry
                            {
                                Email = userReader.GetString(1),
                                Username = userReader.GetString(2),
                                SteamId64 = update.SteamId64,
                                BanType = banType,
                                SendEmail = userReader.GetBoolean(3),
                                SendDiscord = discordEnabled,
                                DiscordWebhooks = discordEnabled && userWebhooks.ContainsKey(userId) 
                                    ? userWebhooks[userId] 
                                    : new List<string>()
                            });
                        }
                    }

                    await transaction.CommitAsync();
                }
                catch (SqlException e)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(e, "Failed to update ban status for {SteamId64}", update.SteamId64);
                    throw;
                }
            }

            return notifications;
        }

        public async Task UpdateLastScannedAt(List<string> steamId64s)
        {
            if (steamId64s == null || steamId64s.Count == 0) return;

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var cmd = new SqlCommand(@"
                UPDATE SteamAccounts
                SET LastScannedAt = GETUTCDATE()
                WHERE SteamId64 IN (SELECT value FROM STRING_SPLIT(@ids, ','));", conn);

            cmd.Parameters.Add("@ids", SqlDbType.NVarChar, -1).Value = string.Join(",", steamId64s);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}

using Microsoft.Data.SqlClient;
using System.Data;
using WebApplication1.Models;

namespace WebApplication1.Repository
{
    public class SteamRepository : ISteamRepository
    {
        private readonly string _connectionString;

        public SteamRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("CONNECTION_STRING")
    ?? throw new InvalidOperationException("Connection string 'CONNECTION_STRING' not found.");
        }




        public async Task TrackSteamAccount(string userId, string steamId64)
        {


            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            using var transaction = conn.BeginTransaction();
            try
            {

                Guid steamAccountId;


                using (var getCmd = new SqlCommand(@"
            SELECT Id 
            FROM SteamAccounts 
            WHERE SteamId64 = @steamId64;", conn, transaction))
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

               
                using (var linkCmd = new SqlCommand(@"
            INSERT INTO UserSteamAccounts (UserId, SteamAccountId)
            VALUES (@userId, @steamAccountId);", conn, transaction))
                {
                    linkCmd.Parameters.Add("@userId", SqlDbType.UniqueIdentifier).Value = userId;
                    linkCmd.Parameters.Add("@steamAccountId", SqlDbType.UniqueIdentifier).Value = steamAccountId;

                    await linkCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();


            }
            catch (SqlException e) when (e.Number == 2627)
            {
                // duplicate insert (already tracked)
                await transaction.RollbackAsync();
            }

            catch (SqlException e)
            {
                Console.WriteLine($"SqlException: {e.Message}");
                throw;
            }
        }













    }
}

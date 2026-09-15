using System.Data;
using System.Reflection;
using Microsoft.Data.SqlClient;

namespace WebApplication1.Migrations
{
    public static class MigrationRunner
    {
        public static async Task ApplyMigrationsAsync(string connectionString, ILogger? logger = null)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            var targetDatabase = builder.InitialCatalog;

            // Step 1: Ensure database exists by connecting to 'master'
            if (!string.IsNullOrWhiteSpace(targetDatabase) &&
                !string.Equals(targetDatabase, "master", StringComparison.OrdinalIgnoreCase))
            {
                var masterBuilder = new SqlConnectionStringBuilder(connectionString)
                {
                    InitialCatalog = "master"
                };

                for (int attempt = 1; attempt <= 5; attempt++)
                {
                    try
                    {
                        using var masterConn = new SqlConnection(masterBuilder.ConnectionString);
                        await masterConn.OpenAsync();

                        using var checkCmd = masterConn.CreateCommand();
                        checkCmd.CommandText = @"
                            IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = @dbName)
                            BEGIN
                                DECLARE @sql NVARCHAR(MAX) = 'CREATE DATABASE [' + REPLACE(@dbName, ']', ']]') + ']';
                                EXEC (@sql);
                            END";
                        checkCmd.Parameters.Add("@dbName", SqlDbType.NVarChar, 128).Value = targetDatabase;
                        await checkCmd.ExecuteNonQueryAsync();
                        break;
                    }
                    catch (SqlException ex) when (attempt < 5)
                    {
                        logger?.LogWarning("Attempt {Attempt} to connect to SQL Server failed ({Message}). Retrying in 2 seconds...", attempt, ex.Message);
                        await Task.Delay(2000);
                    }
                }
            }

            // Step 2: Connect to target database and ensure __MigrationsHistory exists
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using (var historyCmd = conn.CreateCommand())
            {
                historyCmd.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = '__MigrationsHistory')
                    BEGIN
                        CREATE TABLE __MigrationsHistory (
                            MigrationId NVARCHAR(150) PRIMARY KEY,
                            AppliedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
                        );
                    END";
                await historyCmd.ExecuteNonQueryAsync();
            }

            // Step 3: Fetch already applied migrations
            var appliedMigrations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var getHistoryCmd = conn.CreateCommand())
            {
                getHistoryCmd.CommandText = "SELECT MigrationId FROM __MigrationsHistory;";
                using var reader = await getHistoryCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    appliedMigrations.Add(reader.GetString(0));
                }
            }

            // Step 4: Discover migration scripts embedded in assembly
            var assembly = typeof(MigrationRunner).Assembly;
            var resourceNames = assembly.GetManifestResourceNames()
                .Where(name => name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var resourceName in resourceNames)
            {
                var parts = resourceName.Split('.');
                var migrationId = parts.Length >= 2
                    ? $"{parts[^2]}.{parts[^1]}"
                    : resourceName;

                if (appliedMigrations.Contains(migrationId))
                {
                    logger?.LogDebug("Migration {MigrationId} is already applied. Skipping.", migrationId);
                    continue;
                }

                logger?.LogInformation("Applying migration {MigrationId}...", migrationId);

                string sql;
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) continue;
                    using var streamReader = new StreamReader(stream);
                    sql = await streamReader.ReadToEndAsync();
                }

                using var transaction = conn.BeginTransaction();
                try
                {
                    using var applyCmd = conn.CreateCommand();
                    applyCmd.Transaction = transaction;
                    applyCmd.CommandText = sql;
                    await applyCmd.ExecuteNonQueryAsync();

                    using var recordCmd = conn.CreateCommand();
                    recordCmd.Transaction = transaction;
                    recordCmd.CommandText = "INSERT INTO __MigrationsHistory (MigrationId) VALUES (@MigrationId);";
                    recordCmd.Parameters.Add("@MigrationId", SqlDbType.NVarChar, 150).Value = migrationId;
                    await recordCmd.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();
                    logger?.LogInformation("Successfully applied migration {MigrationId}.", migrationId);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    logger?.LogError(ex, "Failed to apply migration {MigrationId}. Transaction rolled back.", migrationId);
                    throw;
                }
            }
        }
    }
}


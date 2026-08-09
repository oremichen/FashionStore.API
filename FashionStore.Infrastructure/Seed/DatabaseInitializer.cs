using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace FashionStore.Infrastructure.Seed;

public static class DatabaseInitializer
{
    private const int MaxDatabaseAttempts = 10;
    private const string MigrationsHistoryTableName = "__EFMigrationsHistory";

    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration, ILogger logger, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<FashionStoreDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await RunMigrationsAsync(context, logger, cancellationToken);
        await Seed.SeedData(roleManager, userManager, configuration, logger);
    }

    private static async Task RunMigrationsAsync(FashionStoreDbContext context, ILogger logger, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxDatabaseAttempts; attempt++)
        {
            try
            {
                var pendingMigrations = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
                if (pendingMigrations.Count == 0)
                {
                    logger.LogInformation("Database is up to date. No pending migrations found.");
                    return;
                }

                logger.LogInformation("Applying {Count} pending database migration(s): {Migrations}", pendingMigrations.Count, string.Join(", ", pendingMigrations));

                await context.Database.MigrateAsync(cancellationToken);

                logger.LogInformation("Database migrations applied successfully.");
                return;
            }
            catch (PostgresException ex) when (attempt < MaxDatabaseAttempts && IsTransient(ex))
            {
                logger.LogWarning(ex, "Database not ready yet. Retrying migration in 5 seconds (attempt {Attempt}/{MaxAttempts}).", attempt, MaxDatabaseAttempts);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch (PostgresException ex) when (IsAlreadyExistsFailure(ex))
            {
                logger.LogWarning(ex, "Migration encountered an existing-schema conflict. Attempting to align EF migration history before retrying.");

                var aligned = await TryAlignMigrationHistoryAsync(context, logger, cancellationToken);
                if (!aligned)
                {
                    throw;
                }

                logger.LogInformation("Database migration history aligned successfully. Skipping reapplication of the existing schema migration.");
                continue;
            }
        }
    }

    private static bool IsTransient(PostgresException ex) =>
        ex.SqlState is PostgresErrorCodes.ConnectionException
            or PostgresErrorCodes.ConnectionDoesNotExist
            or PostgresErrorCodes.ConnectionFailure
            or PostgresErrorCodes.SqlClientUnableToEstablishSqlConnection
            or PostgresErrorCodes.CannotConnectNow
            or PostgresErrorCodes.TooManyConnections
            or PostgresErrorCodes.AdminShutdown
            or PostgresErrorCodes.CrashShutdown;

    private static bool IsAlreadyExistsFailure(PostgresException ex) =>
        ex.SqlState is PostgresErrorCodes.DuplicateTable
            or PostgresErrorCodes.DuplicateObject
            or PostgresErrorCodes.DuplicateColumn;

    private static async Task<bool> TryAlignMigrationHistoryAsync(FashionStoreDbContext context, ILogger logger, CancellationToken cancellationToken)
    {
        var migrationId = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).FirstOrDefault();
        if (migrationId is null)
            return true;

        if (migrationId.EndsWith("_InitialIdentitySetup", StringComparison.Ordinal))
        {
            var requiredTables = new[]
            {
                "AspNetUsers",
                "AspNetRoles",
                "AspNetUserRoles",
                "AspNetUserClaims",
                "AspNetUserLogins",
                "AspNetUserTokens",
                "AspNetRoleClaims"
            };

            foreach (var tableName in requiredTables)
            {
                if (!await TableExistsAsync(context, tableName, cancellationToken))
                {
                    logger.LogWarning(
                        "Cannot align initial Identity migration because required table {TableName} does not exist.",
                        tableName);
                    return false;
                }
            }
        }

        const string qualifiedHistoryTable = "\"__EFMigrationsHistory\"";

        var productVersion = typeof(RelationalDatabaseFacadeExtensions).Assembly.GetName().Version?.ToString(3) ?? "10.0.0";
        var createHistoryTableSql = $"""
            CREATE TABLE IF NOT EXISTS {qualifiedHistoryTable} (
                "MigrationId" character varying(150) NOT NULL,
                "ProductVersion" character varying(32) NOT NULL,
                CONSTRAINT "PK_{MigrationsHistoryTableName}" PRIMARY KEY ("MigrationId")
            );
            """;

        var insertHistorySql =
            $"INSERT INTO {qualifiedHistoryTable} (\"MigrationId\", \"ProductVersion\")" +
             " VALUES ({0}, {1})" +
             " ON CONFLICT (\"MigrationId\") DO NOTHING;";

        await context.Database.ExecuteSqlRawAsync(createHistoryTableSql, cancellationToken);
        await context.Database.ExecuteSqlRawAsync(insertHistorySql, [migrationId, productVersion], cancellationToken);

        logger.LogWarning("Marked existing schema migration {MigrationId} as applied.", migrationId);
        return true;
    }

    private static async Task<bool> TableExistsAsync(FashionStoreDbContext context, string tableName, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = {0}
            ) AS "Value"
            """;

        return await context.Database.SqlQueryRaw<bool>(sql, tableName).SingleAsync(cancellationToken);
    }
}

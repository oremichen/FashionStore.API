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

        await RunMigrationsAsync(context, logger, cancellationToken);
        await Seed.SeedData(context, roleManager, configuration);
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

                var aligned = await TryAlignInitialMigrationHistoryAsync(context, logger, cancellationToken);
                if (!aligned)
                {
                    throw;
                }

                logger.LogInformation("Database migration history aligned successfully. Skipping reapplication of the initial migration.");
                return;
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

    private static async Task<bool> TryAlignInitialMigrationHistoryAsync(FashionStoreDbContext context, ILogger logger, CancellationToken cancellationToken)
    {
        var migrationsAssembly = context.GetService<IMigrationsAssembly>();
        var allMigrations = migrationsAssembly.Migrations.Keys.ToList();
        if (allMigrations.Count != 1)
        {
            logger.LogWarning("Automatic migration history alignment only supports a single initial migration. Found {Count} migrations.", allMigrations.Count);
            return false;
        }

        var initialMigrationId = allMigrations[0];
        var appliedMigrations = (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).ToList();
        if (appliedMigrations.Contains(initialMigrationId))
        {
            logger.LogInformation("Initial migration {MigrationId} is already recorded in migration history.", initialMigrationId);
            return true;
        }

        if (appliedMigrations.Count > 0)
        {
            logger.LogInformation("Migration history already contains other entries, so automatic alignment is being skipped.");
            return true;
        }

        var unexpectedMigrations = appliedMigrations
            .Where(m => m != initialMigrationId)
            .ToList();

        if (unexpectedMigrations.Count > 0)
        {
            logger.LogInformation(
                "Migration history contains unexpected entries: {Entries}. Skipping automatic alignment.",
                string.Join(", ", unexpectedMigrations));
            return false;
        }

        var requiredTables = new[] 
        {   "AspNetUsers", 
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
                logger.LogInformation("Table {TableName} does not exist, so the schema does not look fully initialized yet.", tableName);
                return false;
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
        await context.Database.ExecuteSqlRawAsync(insertHistorySql, [initialMigrationId, productVersion], cancellationToken);

        logger.LogWarning("Marked existing schema as migrated with initial migration {MigrationId}.", initialMigrationId);
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

        var exists = await context.Database.SqlQueryRaw<bool>(sql, tableName).SingleAsync(cancellationToken);
        return exists;
    }
}

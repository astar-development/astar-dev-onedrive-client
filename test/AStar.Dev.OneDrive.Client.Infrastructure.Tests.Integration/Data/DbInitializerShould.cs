using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Integration.Data;

public sealed class DbInitializerShould : IDisposable
{
    private readonly string _dbPath;
    private SqliteConnection? _connection;

    public DbInitializerShould()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"DbInitializerTest_{Guid.NewGuid()}.db");
    }

    public void Dispose()
    {
        _connection?.Dispose();

        // Give SQLite a moment to release file locks (WAL mode creates -wal and -shm files)
        GC.Collect();
        GC.WaitForPendingFinalizers();

        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }

            // Clean up WAL and SHM files
            var walPath = _dbPath + "-wal";
            if (File.Exists(walPath))
            {
                File.Delete(walPath);
            }

            var shmPath = _dbPath + "-shm";
            if (File.Exists(shmPath))
            {
                File.Delete(shmPath);
            }
        }
        catch (IOException)
        {
            // Ignore - test cleanup issue, not a test failure
        }
    }

    [Fact]
    public void CreateDatabaseWhenNotExists()
    {
        DbContextOptionsBuilder<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}");

        using (AppDbContext context = new(options.Options))
        {
            DbInitializer.EnsureDatabaseCreatedAndConfigured(context);
        }

        File.Exists(_dbPath).ShouldBeTrue();
    }

    [Fact]
    public void EnableWALMode()
    {
        DbContextOptionsBuilder<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}");

        using (AppDbContext context = new(options.Options))
        {
            DbInitializer.EnsureDatabaseCreatedAndConfigured(context);
        }

        // Query the journal_mode to verify WAL is enabled
        _connection = new SqliteConnection($"Data Source={_dbPath}");
        _connection.Open();
        
        using SqliteCommand cmd = _connection.CreateCommand();
        cmd.CommandText = "PRAGMA journal_mode;";
        var result = cmd.ExecuteScalar();

        _ = result.ShouldNotBeNull();
        result.ToString()?.ToUpperInvariant().ShouldBe("WAL");
    }

    [Fact]
    public void ApplyMigrations()
    {
        DbContextOptionsBuilder<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}");

        using (AppDbContext context = new(options.Options))
        {
            DbInitializer.EnsureDatabaseCreatedAndConfigured(context);
        }

        // Verify all tables exist
        _connection = new SqliteConnection($"Data Source={_dbPath}");
        _connection.Open();
        
        using SqliteCommand cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
        using SqliteDataReader reader = cmd.ExecuteReader();
        
        List<string> tables = [];
        while (reader.Read())
        {
            tables.Add(reader.GetString(0));
        }
        
        tables.ShouldContain("DriveItems");
        tables.ShouldContain("LocalFiles");
        tables.ShouldContain("DeltaTokens");
        tables.ShouldContain("TransferLogs");
        tables.ShouldContain("__EFMigrationsHistory");
    }

    [Fact]
    public void BeIdempotent()
    {
        DbContextOptionsBuilder<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}");

        using (AppDbContext context = new(options.Options))
        {
            // Call multiple times
            DbInitializer.EnsureDatabaseCreatedAndConfigured(context);
            DbInitializer.EnsureDatabaseCreatedAndConfigured(context);
            DbInitializer.EnsureDatabaseCreatedAndConfigured(context);

            // Should not throw and database should still be functional
            File.Exists(_dbPath).ShouldBeTrue();
            context.Database.CanConnect().ShouldBeTrue();
        }
    }

    [Fact]
    public void LeaveConnectionUsable()
    {
        DbContextOptionsBuilder<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}");

        using (AppDbContext context = new(options.Options))
        {
            DbInitializer.EnsureDatabaseCreatedAndConfigured(context);

            // Verify we can perform database operations after initialization
            DeltaToken token = new("test1", "token123", DateTimeOffset.UtcNow);
            _ = context.DeltaTokens.Add(token);
            var rowsAffected = context.SaveChanges();

            rowsAffected.ShouldBe(1);
        }
    }

    [Fact]
    public async Task WorkWithAsyncOperations()
    {
        DbContextOptionsBuilder<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}");

        using (AppDbContext context = new(options.Options))
        {
            DbInitializer.EnsureDatabaseCreatedAndConfigured(context);

            // Verify async operations work after initialization
            DeltaToken token = new("test1", "token123", DateTimeOffset.UtcNow);
            _ = context.DeltaTokens.Add(token);
            var rowsAffected = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            rowsAffected.ShouldBe(1);

            DeltaToken? retrieved = await context.DeltaTokens.FindAsync(["test1"], TestContext.Current.CancellationToken);
            _ = retrieved.ShouldNotBeNull();
            retrieved.Token.ShouldBe("token123");
        }
    }

    [Fact]
    public void HandleMultipleContextInstancesWithSameDatabase()
    {
        DbContextOptionsBuilder<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_dbPath}");
        
        // Initialize with first context
        using (AppDbContext context1 = new(options.Options))
        {
            DbInitializer.EnsureDatabaseCreatedAndConfigured(context1);
            
            DeltaToken token = new("test1", "token123", DateTimeOffset.UtcNow);
            _ = context1.DeltaTokens.Add(token);
            _ = context1.SaveChanges();
        }

        // Use second context to verify data persists
        using AppDbContext context2 = new(options.Options);
        DbInitializer.EnsureDatabaseCreatedAndConfigured(context2);
        
        DeltaToken? retrieved = context2.DeltaTokens.Find("test1");
        _ = retrieved.ShouldNotBeNull();
        retrieved.Token.ShouldBe("token123");
    }
}

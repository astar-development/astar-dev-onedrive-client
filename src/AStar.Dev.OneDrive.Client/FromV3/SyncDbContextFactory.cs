using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AStar.Dev.OneDrive.Client.FromV3;

/// <summary>
/// Design-time factory for creating SyncDbContext instances during migrations.
/// </summary>
public sealed class SyncDbContextFactory : IDesignTimeDbContextFactory<SyncDbContext>
{
    public SyncDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SyncDbContext>();

        // Use a temporary database path for design-time operations
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dbPath = Path.Combine(appDataPath, "AStar.Dev.OneDrive.Client", "sync.db");

        _ = optionsBuilder.UseSqlite($"Data Source={dbPath}");

        return new SyncDbContext(optionsBuilder.Options);
    }
}

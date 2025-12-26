using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> opts) : DbContext(opts)
{
    public DbSet<DriveItemRecord> DriveItems { get; init; } = null!;
    public DbSet<LocalFileRecord> LocalFiles { get; init; } = null!;
    public DbSet<DeltaToken> DeltaTokens { get; init; } = null!;
    public DbSet<TransferLog> TransferLogs { get; init; } = null!;

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // Explicit per-entity configuration
        _ = mb.ApplyConfiguration(new DriveItemRecordConfiguration());
        _ = mb.ApplyConfiguration(new LocalFileRecordConfiguration());
        _ = mb.ApplyConfiguration(new DeltaTokenConfiguration());
        _ = mb.ApplyConfiguration(new TransferLogConfiguration());

        // Apply SQLite-friendly conversions for the four entities
        mb.UseSqliteFriendlyConversions();
    }
}

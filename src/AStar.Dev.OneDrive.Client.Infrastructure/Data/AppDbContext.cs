using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }
    public AppDbContext() : base(new DbContextOptions<AppDbContext>()) { }

    public DbSet<DriveItemRecord> DriveItems { get; init; } = null!;
    public DbSet<LocalFileRecord> LocalFiles { get; init; } = null!;
    public DbSet<DeltaToken> DeltaTokens { get; init; } = null!;
    public DbSet<TransferLog> TransferLogs { get; init; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Explicit per-entity configuration
        _ = modelBuilder.ApplyConfiguration(new DriveItemRecordConfiguration());
        _ = modelBuilder.ApplyConfiguration(new LocalFileRecordConfiguration());
        _ = modelBuilder.ApplyConfiguration(new DeltaTokenConfiguration());
        _ = modelBuilder.ApplyConfiguration(new TransferLogConfiguration());

        // Apply SQLite-friendly conversions for the four entities
        modelBuilder.UseSqliteFriendlyConversions();
    }
}

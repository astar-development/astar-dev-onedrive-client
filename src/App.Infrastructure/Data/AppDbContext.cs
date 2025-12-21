using Microsoft.EntityFrameworkCore;
using App.Core.Entities;
using App.Infrastructure.Data.Configurations;

namespace App.Infrastructure.Data;

public sealed class AppDbContext : DbContext
{
    public DbSet<DriveItemRecord> DriveItems { get; init; } = null!;
    public DbSet<LocalFileRecord> LocalFiles { get; init; } = null!;
    public DbSet<DeltaToken> DeltaTokens { get; init; } = null!;
    public DbSet<TransferLog> TransferLogs { get; init; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

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

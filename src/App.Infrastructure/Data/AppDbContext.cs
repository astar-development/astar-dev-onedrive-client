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
        mb.ApplyConfiguration(new DriveItemRecordConfiguration());
        mb.ApplyConfiguration(new LocalFileRecordConfiguration());
        mb.ApplyConfiguration(new DeltaTokenConfiguration());
        mb.ApplyConfiguration(new TransferLogConfiguration());

        // Apply SQLite-friendly conversions for the four entities
        mb.UseSqliteFriendlyConversions();
    }
}

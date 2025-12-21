using Microsoft.EntityFrameworkCore;
using App.Core.Entities;

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
        mb.Entity<DriveItemRecord>().HasKey(d => d.Id);
        mb.Entity<LocalFileRecord>().HasKey(l => l.Id);
        mb.Entity<DeltaToken>().HasKey(t => t.Id);
        mb.Entity<TransferLog>().HasKey(t => t.Id);

        mb.Entity<DriveItemRecord>().ToTable("DriveItems");
        mb.Entity<LocalFileRecord>().ToTable("LocalFiles");
        mb.Entity<DeltaToken>().ToTable("DeltaTokens");
        mb.Entity<TransferLog>().ToTable("TransferLogs");

        mb.Entity<DriveItemRecord>().Property(d => d.RelativePath).IsRequired();
        mb.Entity<LocalFileRecord>().Property(l => l.RelativePath).IsRequired();

        // Optional: index for quick lookups by DriveItemId or RelativePath
        mb.Entity<DriveItemRecord>().HasIndex(d => d.DriveItemId);
        mb.Entity<LocalFileRecord>().HasIndex(l => l.RelativePath);
    }
}

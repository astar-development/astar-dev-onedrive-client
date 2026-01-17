using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data;

public class AppDbContext : DbContext
{
    /// <summary>
    /// Gets or sets the accounts table.
    /// </summary>
    public DbSet<AccountEntity> Accounts { get; set; } = null!;

    /// <summary>
    /// Gets or sets the sync configurations table.
    /// </summary>
    public DbSet<SyncConfigurationEntity> SyncConfigurations { get; set; } = null!;

    /// <summary>
    /// Gets or sets the file metadata table.
    /// </summary>
    public DbSet<FileMetadataEntity> FileMetadata { get; set; } = null!;

    /// <summary>
    /// Gets or sets the window preferences table.
    /// </summary>
    public DbSet<WindowPreferencesEntity> WindowPreferences { get; set; } = null!;

    /// <summary>
    /// Gets or sets the sync conflicts table.
    /// </summary>
    public DbSet<SyncConflictEntity> SyncConflicts { get; set; } = null!;

    /// <summary>
    /// Gets or sets the sync session logs table.
    /// </summary>
    public DbSet<SyncSessionLogEntity> SyncSessionLogs { get; set; } = null!;

    /// <summary>
    /// Gets or sets the file operation logs table.
    /// </summary>
    public DbSet<FileOperationLogEntity> FileOperationLogs { get; set; } = null!;

    /// <summary>
    /// Gets or sets the debug logs table.
    /// </summary>
    public DbSet<DebugLogEntity> DebugLogs { get; set; } = null!;

    public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

    public AppDbContext() : base(new DbContextOptions<AppDbContext>()) { }

    public DbSet<DriveItemRecord> DriveItems { get; init; } = null!;

    public DbSet<LocalFileRecord> LocalFiles { get; init; } = null!;

    public DbSet<DeltaToken> DeltaTokens { get; init; } = null!;

    public DbSet<TransferLog> TransferLogs { get; init; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new AccountConfiguration());
        _ = modelBuilder.ApplyConfiguration(new DriveItemRecordConfiguration());
        _ = modelBuilder.ApplyConfiguration(new LocalFileRecordConfiguration());
        _ = modelBuilder.ApplyConfiguration(new DeltaTokenConfiguration());
        _ = modelBuilder.ApplyConfiguration(new TransferLogConfiguration());
        _ = modelBuilder.ApplyConfiguration(new FileMetadataConfiguration());
        _ = modelBuilder.ApplyConfiguration(new SyncConfigurationConfiguration());
        _ = modelBuilder.ApplyConfiguration(new SyncConflictConfiguration());
        _ = modelBuilder.ApplyConfiguration(new WindowPreferencesConfiguration());

        modelBuilder.UseSqliteFriendlyConversions();
    }
}

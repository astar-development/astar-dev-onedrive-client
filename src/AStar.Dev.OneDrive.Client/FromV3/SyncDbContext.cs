using AStar.Dev.OneDrive.Client.FromV3.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.FromV3;

/// <summary>
/// Database context for the OneDrive sync application.
/// </summary>
public sealed class SyncDbContext : DbContext
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

    public SyncDbContext(DbContextOptions<SyncDbContext> options) : base(options)
    {
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(SyncDbContext).Assembly);

        // Configure SyncConfigurationEntity
        _ = modelBuilder.Entity<SyncConfigurationEntity>(entity =>
        {
            _ = entity.HasKey(e => e.Id);
            _ = entity.Property(e => e.AccountId).IsRequired();
            _ = entity.Property(e => e.FolderPath).IsRequired();
            _ = entity.HasIndex(e => new { e.AccountId, e.FolderPath });

            // Foreign key relationship with cascade delete
            _ = entity.HasOne<AccountEntity>()
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure FileMetadataEntity
        _ = modelBuilder.Entity<FileMetadataEntity>(entity =>
        {
            _ = entity.HasKey(e => e.Id);
            _ = entity.Property(e => e.Id).IsRequired();
            _ = entity.Property(e => e.AccountId).IsRequired();
            _ = entity.Property(e => e.Name).IsRequired();
            _ = entity.Property(e => e.Path).IsRequired();
            _ = entity.Property(e => e.LocalPath).IsRequired();

            _ = entity.HasIndex(e => e.AccountId);
            _ = entity.HasIndex(e => new { e.AccountId, e.Path });

            // Foreign key relationship with cascade delete
            _ = entity.HasOne<AccountEntity>()
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure WindowPreferencesEntity
        _ = modelBuilder.Entity<WindowPreferencesEntity>(entity =>
        {
            _ = entity.HasKey(e => e.Id);
            _ = entity.Property(e => e.Width).HasDefaultValue(800);
            _ = entity.Property(e => e.Height).HasDefaultValue(600);
        });

        // Configure SyncConflictEntity
        _ = modelBuilder.Entity<SyncConflictEntity>(entity =>
        {
            _ = entity.HasKey(e => e.Id);
            _ = entity.Property(e => e.Id).IsRequired();
            _ = entity.Property(e => e.AccountId).IsRequired();
            _ = entity.Property(e => e.FilePath).IsRequired();

            _ = entity.HasIndex(e => e.AccountId);
            _ = entity.HasIndex(e => new { e.AccountId, e.IsResolved });

            // Foreign key relationship with cascade delete
            _ = entity.HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

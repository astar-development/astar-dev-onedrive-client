using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.FromV3.Repositories;

/// <summary>
/// Repository implementation for managing sync configuration data.
/// </summary>
public sealed class SyncConfigurationRepository : ISyncConfigurationRepository
{
    private readonly AppDbContext _context;

    public SyncConfigurationRepository(AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SyncConfiguration>> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        List<SyncConfigurationEntity> entities = await _context.SyncConfigurations
            .Where(sc => sc.AccountId == accountId)
            .ToListAsync(cancellationToken);

        return [.. entities.Select(MapToModel)];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetSelectedFoldersAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        return await _context.SyncConfigurations
            .Where(sc => sc.AccountId == accountId && sc.IsSelected)
            .Select(sc => CleanUpPath(sc.FolderPath))
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Result<IList<string>, ErrorResponse>> GetSelectedFolders2Async(string accountId, CancellationToken cancellationToken = default)
            => await _context.SyncConfigurations
                        .Where(sc => sc.AccountId == accountId && sc.IsSelected)
                        .Select(sc => CleanUpPath(sc.FolderPath))
                        .Distinct()
                        .ToListAsync(cancellationToken);

    private static string CleanUpPath(string localFolderPath)
    {
        var indexOfDrives = localFolderPath.IndexOf("drives", StringComparison.OrdinalIgnoreCase);
        if(indexOfDrives >= 0)
        {
            var indexOfColon = localFolderPath.IndexOf(":/", StringComparison.OrdinalIgnoreCase);
            if(indexOfColon > 0)
            {
                var part1 = localFolderPath[..indexOfDrives];
                var part2 = localFolderPath[(indexOfColon + 2)..];
                localFolderPath = part1 + part2;
            }
        }

        return localFolderPath;
    }

    /// <inheritdoc/>
    public async Task<SyncConfiguration> AddAsync(SyncConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        SyncConfigurationEntity? existingEntity = await _context.SyncConfigurations
            .FirstOrDefaultAsync(sc => sc.AccountId == configuration.AccountId && sc.FolderPath == configuration.FolderPath, cancellationToken);

        if(existingEntity is not null)
        {
            return configuration;
        }

        var lastIndexOf = configuration.FolderPath.LastIndexOf('/');
        if(lastIndexOf > 0)
        {
            var parentPath = configuration.FolderPath[..lastIndexOf];
            var test =  SyncEngine.FormatScanningFolderForDisplay(parentPath)!.Replace("OneDrive: ", string.Empty);
            SyncConfigurationEntity? parentEntity = await _context.SyncConfigurations
                .FirstOrDefaultAsync(sc => sc.AccountId == configuration.AccountId && (sc.FolderPath == parentPath || sc.FolderPath == test), cancellationToken);

            if(parentEntity is not null)
            {
                var updatedPath = SyncEngine.FormatScanningFolderForDisplay(configuration.FolderPath)!.Replace("OneDrive: ", string.Empty);
                configuration = configuration with { FolderPath = updatedPath, IsSelected = parentEntity.IsSelected };
            }
        }

        SyncConfigurationEntity entity = MapToEntity(configuration);
        _ = _context.SyncConfigurations.Add(entity);
        _ = await _context.SaveChangesAsync(cancellationToken);

        return configuration;
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(SyncConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        SyncConfigurationEntity entity = await _context.SyncConfigurations.FindAsync([configuration.Id], cancellationToken) ?? throw new InvalidOperationException($"Sync configuration with ID '{configuration.Id}' not found.");

        entity.AccountId = configuration.AccountId;
        entity.FolderPath = configuration.FolderPath;
        entity.IsSelected = configuration.IsSelected;
        entity.LastModifiedUtc = configuration.LastModifiedUtc;

        _ = await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        SyncConfigurationEntity? entity = await _context.SyncConfigurations.FindAsync([id], cancellationToken);
        if(entity is not null)
        {
            _ = _context.SyncConfigurations.Remove(entity);
            _ = await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        List<SyncConfigurationEntity> entities = await _context.SyncConfigurations
            .Where(sc => sc.AccountId == accountId)
            .ToListAsync(cancellationToken);

        _context.SyncConfigurations.RemoveRange(entities);
        _ = await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SaveBatchAsync(string accountId, IEnumerable<SyncConfiguration> configurations, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentNullException.ThrowIfNull(configurations);

        List<SyncConfigurationEntity> existingEntities = await _context.SyncConfigurations
            .Where(sc => sc.AccountId == accountId)
            .ToListAsync(cancellationToken);

        _context.SyncConfigurations.RemoveRange(existingEntities);

        var newEntities = configurations.Select(MapToEntity).ToList();
        _context.SyncConfigurations.AddRange(newEntities);

        _ = await _context.SaveChangesAsync(cancellationToken);
    }

    private static SyncConfiguration MapToModel(SyncConfigurationEntity entity)
        => new(
            entity.Id,
            entity.AccountId,
            entity.FolderPath,
            entity.IsSelected,
            entity.LastModifiedUtc
        );

    private static SyncConfigurationEntity MapToEntity(SyncConfiguration model)
        => new()
        {
            Id = model.Id,
            AccountId = model.AccountId,
            FolderPath = model.FolderPath,
            IsSelected = model.IsSelected,
            LastModifiedUtc = model.LastModifiedUtc
        };
}

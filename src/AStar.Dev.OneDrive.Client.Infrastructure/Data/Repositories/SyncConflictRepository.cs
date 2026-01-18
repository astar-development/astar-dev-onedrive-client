using AStar.Dev.OneDrive.Client.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;

/// <summary>
/// Repository for managing sync conflicts in the database.
/// </summary>
public sealed class SyncConflictRepository : ISyncConflictRepository
{
    private readonly AppDbContext _context;

    public SyncConflictRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SyncConflict>> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        List<SyncConflictEntity> entities = await _context.SyncConflicts
            .Where(c => c.AccountId == accountId)
            .OrderByDescending(c => c.DetectedUtc)
            .ToListAsync(cancellationToken);

        return [.. entities.Select(MapToDomain)];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SyncConflict>> GetUnresolvedByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        List<SyncConflictEntity> entities = await _context.SyncConflicts
            .Where(c => c.AccountId == accountId && !c.IsResolved)
            .OrderByDescending(c => c.DetectedUtc)
            .ToListAsync(cancellationToken);

        return [.. entities.Select(MapToDomain)];
    }

    /// <inheritdoc/>
    public async Task<SyncConflict?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        SyncConflictEntity? entity = await _context.SyncConflicts
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return entity is not null ? MapToDomain(entity) : null;
    }

    /// <inheritdoc/>
    public async Task<SyncConflict?> GetByFilePathAsync(string accountId, string filePath, CancellationToken cancellationToken = default)
    {
        SyncConflictEntity? entity = await _context.SyncConflicts
            .Where(c => c.AccountId == accountId && c.FilePath == filePath && !c.IsResolved)
            .OrderByDescending(c => c.DetectedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is not null ? MapToDomain(entity) : null;
    }

    /// <inheritdoc/>
    public async Task AddAsync(SyncConflict conflict, CancellationToken cancellationToken = default)
    {
        SyncConflictEntity entity = MapToEntity(conflict);
        _ = await _context.SyncConflicts.AddAsync(entity, cancellationToken);
        _ = await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(SyncConflict conflict, CancellationToken cancellationToken = default)
    {
        SyncConflictEntity existingEntity = await _context.SyncConflicts
            .FirstOrDefaultAsync(c => c.Id == conflict.Id, cancellationToken) ?? throw new InvalidOperationException($"Conflict not found: {conflict.Id}");

        existingEntity.ResolutionStrategy = conflict.ResolutionStrategy;
        existingEntity.IsResolved = conflict.IsResolved;

        _ = await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        SyncConflictEntity? entity = await _context.SyncConflicts
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if(entity is not null)
        {
            _ = _context.SyncConflicts.Remove(entity);
            _ = await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteByAccountIdAsync(string accountId, CancellationToken cancellationToken = default) => _ = await _context.SyncConflicts
            .Where(c => c.AccountId == accountId)
            .ExecuteDeleteAsync(cancellationToken);

    private static SyncConflict MapToDomain(SyncConflictEntity entity)
        => new SyncConflict( 
            entity.Id,
             entity.AccountId,
             entity.FilePath,
             entity.LocalModifiedUtc,
             entity.RemoteModifiedUtc,
             entity.LocalSize,
             entity.RemoteSize,
             entity.DetectedUtc,
             entity.ResolutionStrategy,
             entity.IsResolved);

    private static SyncConflictEntity MapToEntity(SyncConflict conflict)
        => new()
        {
            Id = conflict.Id,
            AccountId = conflict.AccountId,
            FilePath = conflict.FilePath,
            LocalModifiedUtc = conflict.LocalModifiedUtc,
            RemoteModifiedUtc = conflict.RemoteModifiedUtc,
            LocalSize = conflict.LocalSize,
            RemoteSize = conflict.RemoteSize,
            DetectedUtc = conflict.DetectedUtc,
            ResolutionStrategy = conflict.ResolutionStrategy,
            IsResolved = conflict.IsResolved
        };
}

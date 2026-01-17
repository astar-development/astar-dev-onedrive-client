using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.FromV3.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.FromV3.Repositories;

/// <summary>
/// Repository implementation for managing sync session logs.
/// </summary>
public sealed class SyncSessionLogRepository : ISyncSessionLogRepository
{
    private readonly AppDbContext _context;

    public SyncSessionLogRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SyncSessionLog>> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        List<SyncSessionLogEntity> entities = await _context.SyncSessionLogs
            .AsNoTracking()
            .Where(s => s.AccountId == accountId)
            .OrderByDescending(s => s.StartedUtc)
            .ToListAsync(cancellationToken);

        return [.. entities.Select(MapToModel)];
    }

    /// <inheritdoc/>
    public async Task<SyncSessionLog?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        SyncSessionLogEntity? entity = await _context.SyncSessionLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return entity is not null ? MapToModel(entity) : null;
    }

    /// <inheritdoc/>
    public async Task AddAsync(SyncSessionLog sessionLog, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sessionLog);

        SyncSessionLogEntity entity = MapToEntity(sessionLog);
        _ = _context.SyncSessionLogs.Add(entity);
        _ = await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(SyncSessionLog sessionLog, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sessionLog);

        SyncSessionLogEntity entity = await _context.SyncSessionLogs.FindAsync([sessionLog.Id], cancellationToken) ?? throw new InvalidOperationException($"Sync session log with ID '{sessionLog.Id}' not found.");

        entity.CompletedUtc = sessionLog.CompletedUtc;
        entity.Status = (int)sessionLog.Status;
        entity.FilesUploaded = sessionLog.FilesUploaded;
        entity.FilesDownloaded = sessionLog.FilesDownloaded;
        entity.FilesDeleted = sessionLog.FilesDeleted;
        entity.ConflictsDetected = sessionLog.ConflictsDetected;
        entity.TotalBytes = sessionLog.TotalBytes;

        _ = await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteOldSessionsAsync(string accountId, DateTime olderThan, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        _ = await _context.SyncSessionLogs
            .Where(s => s.AccountId == accountId && s.StartedUtc < olderThan)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static SyncSessionLog MapToModel(SyncSessionLogEntity entity)
        => new(
            entity.Id,
            entity.AccountId,
            entity.StartedUtc,
            entity.CompletedUtc,
            (SyncStatus)entity.Status,
            entity.FilesUploaded,
            entity.FilesDownloaded,
            entity.FilesDeleted,
            entity.ConflictsDetected,
            entity.TotalBytes);

    private static SyncSessionLogEntity MapToEntity(SyncSessionLog model)
        => new()
        {
            Id = model.Id,
            AccountId = model.AccountId,
            StartedUtc = model.StartedUtc,
            CompletedUtc = model.CompletedUtc,
            Status = (int)model.Status,
            FilesUploaded = model.FilesUploaded,
            FilesDownloaded = model.FilesDownloaded,
            FilesDeleted = model.FilesDeleted,
            ConflictsDetected = model.ConflictsDetected,
            TotalBytes = model.TotalBytes
        };
}

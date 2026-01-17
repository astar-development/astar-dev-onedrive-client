using AStar.Dev.OneDrive.Client.FromV3.Entities;
using AStar.Dev.OneDrive.Client.FromV3.Models;
using AStar.Dev.OneDrive.Client.FromV3.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.FromV3.Repositories;

/// <summary>
/// Repository implementation for managing file operation logs.
/// </summary>
public sealed class FileOperationLogRepository : IFileOperationLogRepository
{
    private readonly SyncDbContext _context;

    public FileOperationLogRepository(SyncDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FileOperationLog>> GetBySessionIdAsync(string syncSessionId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(syncSessionId);

        List<FileOperationLogEntity> entities = await _context.FileOperationLogs
            .AsNoTracking()
            .Where(f => f.SyncSessionId == syncSessionId)
            .OrderBy(f => f.Timestamp)
            .ToListAsync(cancellationToken);

        return [.. entities.Select(MapToModel)];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FileOperationLog>> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        List<FileOperationLogEntity> entities = await _context.FileOperationLogs
            .AsNoTracking()
            .Where(f => f.AccountId == accountId)
            .OrderByDescending(f => f.Timestamp)
            .ToListAsync(cancellationToken);

        return [.. entities.Select(MapToModel)];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FileOperationLog>> GetByAccountIdAsync(string accountId, int pageSize, int skip, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        List<FileOperationLogEntity> entities = await _context.FileOperationLogs
            .AsNoTracking()
            .Where(f => f.AccountId == accountId)
            .OrderByDescending(f => f.Timestamp)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return [.. entities.Select(MapToModel)];
    }

    /// <inheritdoc/>
    public async Task AddAsync(FileOperationLog operationLog, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operationLog);

        FileOperationLogEntity entity = MapToEntity(operationLog);
        _ = _context.FileOperationLogs.Add(entity);
        _ = await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteOldOperationsAsync(string accountId, DateTime olderThan, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        _ = await _context.FileOperationLogs
            .Where(f => f.AccountId == accountId && f.Timestamp < olderThan)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static FileOperationLog MapToModel(FileOperationLogEntity entity)
        => new(
            entity.Id,
            entity.SyncSessionId,
            entity.AccountId,
            entity.Timestamp,
            (FileOperation)entity.Operation,
            entity.FilePath,
            entity.LocalPath,
            entity.OneDriveId,
            entity.FileSize,
            entity.LocalHash,
            entity.RemoteHash,
            entity.LastModifiedUtc,
            entity.Reason);

    private static FileOperationLogEntity MapToEntity(FileOperationLog model)
        => new()
        {
            Id = model.Id,
            SyncSessionId = model.SyncSessionId,
            AccountId = model.AccountId,
            Timestamp = model.Timestamp,
            Operation = (int)model.Operation,
            FilePath = model.FilePath,
            LocalPath = model.LocalPath,
            OneDriveId = model.OneDriveId,
            FileSize = model.FileSize,
            LocalHash = model.LocalHash,
            RemoteHash = model.RemoteHash,
            LastModifiedUtc = model.LastModifiedUtc,
            Reason = model.Reason
        };
}

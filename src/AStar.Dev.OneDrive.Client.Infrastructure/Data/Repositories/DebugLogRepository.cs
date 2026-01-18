using AStar.Dev.OneDrive.Client.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for accessing debug log entries.
/// </summary>
public sealed class DebugLogRepository : IDebugLogRepository
{
    private readonly AppDbContext _context;

    public DebugLogRepository(AppDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DebugLogEntry>> GetByAccountIdAsync(string accountId, int pageSize, int skip, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        List<DebugLogEntity> entities = await _context.DebugLogs
            .Where(log => log.AccountId == accountId)
            .OrderByDescending(log => log.TimestampUtc)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return [.. entities.Select(entity => new DebugLogEntry(
            entity.Id,
            entity.AccountId,
            entity.TimestampUtc,
            entity.LogLevel,
            entity.Source,
            entity.Message,
            entity.Exception
        ))];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DebugLogEntry>> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        List<DebugLogEntity> entities = await _context.DebugLogs
            .Where(log => log.AccountId == accountId)
            .OrderByDescending(log => log.TimestampUtc)
            .ToListAsync(cancellationToken);

        return [.. entities.Select(entity => new DebugLogEntry(
            entity.Id,
            entity.AccountId,
            entity.TimestampUtc,
            entity.LogLevel,
            entity.Source,
            entity.Message,
            entity.Exception
        ))];
    }

    /// <inheritdoc/>
    public async Task DeleteByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        List<DebugLogEntity> entities = await _context.DebugLogs
            .Where(log => log.AccountId == accountId)
            .ToListAsync(cancellationToken);

        _context.DebugLogs.RemoveRange(entities);
        _ = await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteOlderThanAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        List<DebugLogEntity> entities = await _context.DebugLogs
            .Where(log => log.TimestampUtc < olderThan)
            .ToListAsync(cancellationToken);

        _context.DebugLogs.RemoveRange(entities);
        _ = await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetDebugLogCountByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
        => await _context.DebugLogs
                .Where(log => log.AccountId == accountId)
                .CountAsync(cancellationToken);
}

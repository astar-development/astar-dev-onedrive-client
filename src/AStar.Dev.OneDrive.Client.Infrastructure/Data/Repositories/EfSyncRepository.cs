using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;

public sealed class EfSyncRepository : ISyncRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<EfSyncRepository> _logger;

    public EfSyncRepository(AppDbContext db, ILogger<EfSyncRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<DeltaToken?> GetDeltaTokenAsync(CancellationToken ct)
        => await _db.DeltaTokens.OrderByDescending(t => t.LastSyncedUtc).FirstOrDefaultAsync(ct);

    public async Task SaveOrUpdateDeltaTokenAsync(DeltaToken token, CancellationToken ct)
    {
        DeltaToken? existing = await _db.DeltaTokens.FindAsync(new object[] { token.Id }, ct);
        if(existing is null)
            _ = _db.DeltaTokens.Add(token);
        else
            _db.Entry(existing).CurrentValues.SetValues(token);
        _ = await _db.SaveChangesAsync(ct);
    }

    public async Task ApplyDriveItemsAsync(IEnumerable<DriveItemRecord> items, CancellationToken ct)
    {
        // Batch apply inside a transaction to reduce contention on SQLite
        await using IDbContextTransaction tx = await _db.Database.BeginTransactionAsync(ct);
        foreach(DriveItemRecord item in items)
        {
            DriveItemRecord? existing = await _db.DriveItems.FindAsync(new object[] { item.Id }, ct);
            if(existing is null)
                _ = _db.DriveItems.Add(item);
            else
                _db.Entry(existing).CurrentValues.SetValues(item);
        }

        _ = await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<IEnumerable<DriveItemRecord>> GetPendingDownloadsAsync(int pageSize, int offset, CancellationToken ct)
    {
        var totalDriveItems = await _db.DriveItems.CountAsync(ct);
        var totalFiles = await _db.DriveItems.Where(d => !d.IsFolder && !d.IsDeleted).CountAsync(ct);
        var downloadedFiles = await _db.LocalFiles.Where(l => l.SyncState == SyncState.Downloaded || l.SyncState == SyncState.Uploaded).CountAsync(ct);

        _logger.LogDebug("Repository stats: {TotalItems} total items, {TotalFiles} files, {Downloaded} already downloaded", 
            totalDriveItems, totalFiles, downloadedFiles);

        var query = _db.DriveItems
                            .Where(d => !d.IsFolder && !d.IsDeleted)
                            .Where(d => !_db.LocalFiles.Any(l => l.Id == d.Id && (l.SyncState == SyncState.Downloaded || l.SyncState == SyncState.Uploaded)))
                            .OrderBy(d => d.LastModifiedUtc)
                            .Skip(offset*pageSize)
                            .Take(pageSize);

        var results = await query.ToListAsync(ct);

        _logger.LogDebug("GetPendingDownloadsAsync(pageSize={PageSize}, offset={Offset}): returning {Count} items", 
            pageSize, offset, results.Count);

        return results;
    }

    public async Task<int> GetPendingDownloadCountAsync(CancellationToken ct)
    {
        var count = await _db.DriveItems
                            .Where(d => !d.IsFolder && !d.IsDeleted)
                            .Where(d => !_db.LocalFiles.Any(l => l.Id == d.Id && (l.SyncState == SyncState.Downloaded || l.SyncState == SyncState.Uploaded)))
                            .CountAsync(ct);

        _logger.LogDebug("GetPendingDownloadCountAsync: {Count} pending downloads", count);

        return count;
    }

    public async Task MarkLocalFileStateAsync(string driveItemId, SyncState state, CancellationToken ct)
    {
        // Use driveItemId as the local file id mapping for simplicity
        DriveItemRecord? drive = await _db.DriveItems.FindAsync(new object[] { driveItemId }, ct);
        if(drive is null)
            return;

        LocalFileRecord? local = await _db.LocalFiles.FindAsync(new object[] { driveItemId }, ct);
        if(local is null)
        {
            _ = _db.LocalFiles.Add(new LocalFileRecord(driveItemId, drive.RelativePath, null, drive.Size, drive.LastModifiedUtc, state));
        }
        else
        {
            _db.Entry(local).CurrentValues.SetValues(local with { SyncState = state, LastWriteUtc = drive.LastModifiedUtc, Size = drive.Size });
        }

        _ = await _db.SaveChangesAsync(ct);
    }

    public async Task AddOrUpdateLocalFileAsync(LocalFileRecord file, CancellationToken ct)
    {
        LocalFileRecord? existing = await _db.LocalFiles.FindAsync(new object[] { file.Id }, ct);
        if(existing is null)
            _ = _db.LocalFiles.Add(file);
        else
            _db.Entry(existing).CurrentValues.SetValues(file);
        _ = await _db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<LocalFileRecord>> GetPendingUploadsAsync(int limit, CancellationToken ct)
        => await _db.LocalFiles.Where(l => l.SyncState == SyncState.PendingUpload).Take(limit).ToListAsync(ct);

    public async Task<int> GetPendingUploadCountAsync(CancellationToken ct)
        => await _db.LocalFiles.Where(l => l.SyncState == SyncState.PendingUpload).CountAsync(ct);

    public async Task<DriveItemRecord?> GetDriveItemByPathAsync(string relativePath, CancellationToken ct)
        => await _db.DriveItems.FirstOrDefaultAsync(d => d.RelativePath == relativePath && !d.IsDeleted, ct);
    public async Task<LocalFileRecord?> GetLocalFileByPathAsync(string relativePath, CancellationToken ct)
        => await _db.LocalFiles.FirstOrDefaultAsync(l => l.RelativePath == relativePath, ct);

    public async Task LogTransferAsync(TransferLog log, CancellationToken ct)
    {
        TransferLog? existing = await _db.TransferLogs.FindAsync(new object[] { log.Id }, ct);
        if(existing is not null)
        {
            _db.Entry(existing).CurrentValues.SetValues(log);
            _ = await _db.SaveChangesAsync(ct);
            return;
        }

        _ = _db.TransferLogs.Add(log);
        _ = await _db.SaveChangesAsync(ct);
    }
}

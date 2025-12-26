using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;

public sealed class EfSyncRepository(AppDbContext db, ILogger<EfSyncRepository> logger) : ISyncRepository
{
    public async Task<DeltaToken?> GetDeltaTokenAsync(CancellationToken ct)
        => await db.DeltaTokens.OrderByDescending(t => t.LastSyncedUtc).FirstOrDefaultAsync(ct);

    public async Task SaveOrUpdateDeltaTokenAsync(DeltaToken token, CancellationToken ct)
    {
        DeltaToken? existing = await db.DeltaTokens.FindAsync([token.Id], ct);
        if(existing is null)
            _ = db.DeltaTokens.Add(token);
        else
            db.Entry(existing).CurrentValues.SetValues(token);
        _ = await db.SaveChangesAsync(ct);
    }

    public async Task ApplyDriveItemsAsync(IEnumerable<DriveItemRecord> items, CancellationToken ct)
    {
        // Batch apply inside a transaction to reduce contention on SQLite
        await using IDbContextTransaction tx = await db.Database.BeginTransactionAsync(ct);
        foreach(DriveItemRecord item in items)
        {
            DriveItemRecord? existing = await db.DriveItems.FindAsync([item.Id], ct);
            if(existing is null)
                _ = db.DriveItems.Add(item);
            else
                db.Entry(existing).CurrentValues.SetValues(item);
        }

        _ = await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<IEnumerable<DriveItemRecord>> GetPendingDownloadsAsync(int pageSize, int offset, CancellationToken ct)
    {
        var totalDriveItems = await db.DriveItems.CountAsync(ct);
        var totalFiles = await db.DriveItems.Where(d => !d.IsFolder && !d.IsDeleted).CountAsync(ct);
        var downloadedFiles = await db.LocalFiles.Where(l => l.SyncState == SyncState.Downloaded || l.SyncState == SyncState.Uploaded).CountAsync(ct);

        logger.LogDebug("Repository stats: {TotalItems} total items, {TotalFiles} files, {Downloaded} already downloaded",
            totalDriveItems, totalFiles, downloadedFiles);

        IQueryable<DriveItemRecord> query = db.DriveItems
                            .Where(d => !d.IsFolder && !d.IsDeleted)
                            .Where(d => !db.LocalFiles.Any(l => l.Id == d.Id && (l.SyncState == SyncState.Downloaded || l.SyncState == SyncState.Uploaded)))
                            .OrderBy(d => d.LastModifiedUtc)
                            .Skip(offset*pageSize)
                            .Take(pageSize);

        List<DriveItemRecord> results = await query.ToListAsync(ct);

        logger.LogDebug("GetPendingDownloadsAsync(pageSize={PageSize}, offset={Offset}): returning {Count} items",
            pageSize, offset, results.Count);

        return results;
    }

    public async Task<int> GetPendingDownloadCountAsync(CancellationToken ct)
    {
        var count = await db.DriveItems
                            .Where(d => !d.IsFolder && !d.IsDeleted)
                            .Where(d => !db.LocalFiles.Any(l => l.Id == d.Id && (l.SyncState == SyncState.Downloaded || l.SyncState == SyncState.Uploaded)))
                            .CountAsync(ct);

        logger.LogDebug("GetPendingDownloadCountAsync: {Count} pending downloads", count);

        return count;
    }

    public async Task MarkLocalFileStateAsync(string driveItemId, SyncState state, CancellationToken ct)
    {
        // Use driveItemId as the local file id mapping for simplicity
        DriveItemRecord? drive = await db.DriveItems.FindAsync([driveItemId], ct);
        if(drive is null)
            return;

        LocalFileRecord? local = await db.LocalFiles.FindAsync([driveItemId], ct);
        if(local is null)
        {
            _ = db.LocalFiles.Add(new LocalFileRecord(driveItemId, drive.RelativePath, null, drive.Size, drive.LastModifiedUtc, state));
        }
        else
        {
            db.Entry(local).CurrentValues.SetValues(local with { SyncState = state, LastWriteUtc = drive.LastModifiedUtc, Size = drive.Size });
        }

        _ = await db.SaveChangesAsync(ct);
    }

    public async Task AddOrUpdateLocalFileAsync(LocalFileRecord file, CancellationToken ct)
    {
        LocalFileRecord? existing = await db.LocalFiles.FindAsync([file.Id], ct);
        if(existing is null)
            _ = db.LocalFiles.Add(file);
        else
            db.Entry(existing).CurrentValues.SetValues(file);
        _ = await db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<LocalFileRecord>> GetPendingUploadsAsync(int limit, CancellationToken ct)
        => await db.LocalFiles.Where(l => l.SyncState == SyncState.PendingUpload).Take(limit).ToListAsync(ct);

    public async Task<int> GetPendingUploadCountAsync(CancellationToken ct)
        => await db.LocalFiles.Where(l => l.SyncState == SyncState.PendingUpload).CountAsync(ct);

    public async Task<DriveItemRecord?> GetDriveItemByPathAsync(string relativePath, CancellationToken ct)
        => await db.DriveItems.FirstOrDefaultAsync(d => d.RelativePath == relativePath && !d.IsDeleted, ct);
    public async Task<LocalFileRecord?> GetLocalFileByPathAsync(string relativePath, CancellationToken ct)
        => await db.LocalFiles.FirstOrDefaultAsync(l => l.RelativePath == relativePath, ct);

    public async Task LogTransferAsync(TransferLog log, CancellationToken ct)
    {
        TransferLog? existing = await db.TransferLogs.FindAsync([log.Id], ct);
        if(existing is not null)
        {
            db.Entry(existing).CurrentValues.SetValues(log);
            _ = await db.SaveChangesAsync(ct);
            return;
        }

        _ = db.TransferLogs.Add(log);
        _ = await db.SaveChangesAsync(ct);
    }
}

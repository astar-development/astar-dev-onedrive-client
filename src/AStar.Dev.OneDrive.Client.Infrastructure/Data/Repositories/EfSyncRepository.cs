using AStar.Dev.OneDrive.Client.Core.Entities;
using AStar.Dev.OneDrive.Client.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;

public sealed class EfSyncRepository(AppDbContext db, ILogger<EfSyncRepository> logger) : ISyncRepository
{
    public async Task<DeltaToken?> GetDeltaTokenAsync(CancellationToken cancellationToken)
        => await db.DeltaTokens.OrderByDescending(t => t.LastSyncedUtc).FirstOrDefaultAsync(cancellationToken);

    public async Task SaveOrUpdateDeltaTokenAsync(DeltaToken token, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DeltaToken? existing = await db.DeltaTokens.FindAsync([token.Id], cancellationToken);
        if(existing is null)
            _ = db.DeltaTokens.Add(token);
        else
            db.Entry(existing).CurrentValues.SetValues(token);
        _ = await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ApplyDriveItemsAsync(IEnumerable<DriveItemRecord> items, CancellationToken cancellationToken)
    {
        await using IDbContextTransaction tx = await db.Database.BeginTransactionAsync(cancellationToken);
        foreach(DriveItemRecord item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DriveItemRecord? existing = await db.DriveItems.FindAsync([item.Id], cancellationToken);
            if(existing is null)
                _ = db.DriveItems.Add(item);
            else
                db.Entry(existing).CurrentValues.SetValues(item);
        }

        _ = await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    public async Task<IEnumerable<DriveItemRecord>> GetPendingDownloadsAsync(int pageSize, int offset, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var totalDriveItems = await db.DriveItems.CountAsync(cancellationToken);
        var totalFiles = await db.DriveItems.Where(d => !d.IsFolder && !d.IsDeleted).CountAsync(cancellationToken);
        var downloadedFiles = await db.LocalFiles.Where(l => l.SyncState == SyncState.Downloaded || l.SyncState == SyncState.Uploaded).CountAsync(cancellationToken);

        logger.LogDebug("Repository stats: {TotalItems} total items, {TotalFiles} files, {Downloaded} already downloaded",
            totalDriveItems, totalFiles, downloadedFiles);
        var stats = string.Format("Repository stats: {0} total items, {1} files, {2} already downloaded",
            totalDriveItems, totalFiles, downloadedFiles);
        var log = new TransferLog(Guid.CreateVersion7().ToString(), TransferType.Download, "Stats", DateTimeOffset.UtcNow, null, TransferStatus.InProgress, 0, stats);

        _ = db.TransferLogs.Add(log);

        IQueryable<DriveItemRecord> driveItems = db.DriveItems
                            .Where(d => !d.IsFolder && !d.IsDeleted)
                            .Where(d => !db.LocalFiles.Any(l => l.Id == d.Id && (l.SyncState == SyncState.Downloaded || l.SyncState == SyncState.Uploaded)))
                            .OrderBy(d => d.LastModifiedUtc)
                            .Skip(offset*pageSize)
                            .Take(pageSize);

        List<DriveItemRecord> results = await driveItems.ToListAsync(cancellationToken);

        logger.LogDebug("GetPendingDownloadsAsync(pageSize={PageSize}, offset={Offset}): returning {Count} items",
            pageSize, offset, results.Count);

        var stats2 = string.Format("GetPendingDownloadsAsync(pageSize={0}, offset={1}): returning {2} items",
            pageSize, offset, results.Count);
        var log2 = new TransferLog(Guid.CreateVersion7().ToString(), TransferType.Download, "Stats2", DateTimeOffset.UtcNow, null, TransferStatus.InProgress, 0, stats2);

        _ = db.TransferLogs.Add(log2);
        _ = await db.SaveChangesAsync(cancellationToken);

        return results;
    }

    public async Task<int> GetPendingDownloadCountAsync(CancellationToken cancellationToken)
    {
        var count = await db.DriveItems
                            .Where(d => !d.IsFolder && !d.IsDeleted)
                            .Where(d => !db.LocalFiles.Any(l => l.Id == d.Id && (l.SyncState == SyncState.Downloaded || l.SyncState == SyncState.Uploaded)))
                            .CountAsync(cancellationToken);

        logger.LogDebug("GetPendingDownloadCountAsync: {Count} pending downloads", count);

        return count;
    }

    public async Task MarkLocalFileStateAsync(string driveItemId, SyncState state, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DriveItemRecord? drive = await db.DriveItems.FindAsync([driveItemId], cancellationToken);
        if(drive is null)
            return;

        LocalFileRecord? local = await db.LocalFiles.FindAsync([driveItemId], cancellationToken);
        if(local is null)
        {
            _ = db.LocalFiles.Add(new LocalFileRecord(driveItemId, drive.RelativePath, null, drive.Size, drive.LastModifiedUtc, state));
        }
        else
        {
            db.Entry(local).CurrentValues.SetValues(local with { SyncState = state, LastWriteUtc = drive.LastModifiedUtc, Size = drive.Size });
        }

        _ = await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddOrUpdateLocalFileAsync(LocalFileRecord file, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LocalFileRecord? existing = await db.LocalFiles.FindAsync([file.Id], cancellationToken);
        if(existing is null)
            _ = db.LocalFiles.Add(file);
        else
            db.Entry(existing).CurrentValues.SetValues(file);
        _ = await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<LocalFileRecord>> GetPendingUploadsAsync(int limit, CancellationToken cancellationToken)
        => await db.LocalFiles.Where(l => l.SyncState == SyncState.PendingUpload).Take(limit).ToListAsync(cancellationToken);

    public async Task<int> GetPendingUploadCountAsync(CancellationToken cancellationToken)
        => await db.LocalFiles.Where(l => l.SyncState == SyncState.PendingUpload).CountAsync(cancellationToken);

    public async Task<DriveItemRecord?> GetDriveItemByPathAsync(string relativePath, CancellationToken cancellationToken)
        => await db.DriveItems.FirstOrDefaultAsync(d => d.RelativePath == relativePath && !d.IsDeleted, cancellationToken);
    public async Task<LocalFileRecord?> GetLocalFileByPathAsync(string relativePath, CancellationToken cancellationToken)
        => await db.LocalFiles.FirstOrDefaultAsync(l => l.RelativePath == relativePath, cancellationToken);

    public async Task LogTransferAsync(TransferLog log, CancellationToken cancellationToken)
    {
        TransferLog? existing = await db.TransferLogs.FindAsync([log.Id], cancellationToken);
        if(existing is not null)
        {
            db.Entry(existing).CurrentValues.SetValues(log);
            _ = await db.SaveChangesAsync(cancellationToken);
            return;
        }

        _ = db.TransferLogs.Add(log);
        _ = await db.SaveChangesAsync(cancellationToken);
    }
}

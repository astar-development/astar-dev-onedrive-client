using Microsoft.EntityFrameworkCore;
using App.Core.Interfaces;
using App.Core.Entities;
using App.Infrastructure.Data;

namespace App.Infrastructure.Repositories;

public sealed class EfSyncRepository : ISyncRepository
{
    private readonly AppDbContext _db;
    public EfSyncRepository(AppDbContext db) => _db = db;

    public async Task<DeltaToken?> GetDeltaTokenAsync(CancellationToken ct) =>
        await _db.DeltaTokens.OrderByDescending(t => t.LastSyncedUtc).FirstOrDefaultAsync(ct);

    public async Task SaveOrUpdateDeltaTokenAsync(DeltaToken token, CancellationToken ct)
    {
        var existing = await _db.DeltaTokens.FindAsync(new object[] { token.Id }, ct);
        if (existing is null) _db.DeltaTokens.Add(token);
        else _db.Entry(existing).CurrentValues.SetValues(token);
        await _db.SaveChangesAsync(ct);
    }

    public async Task ApplyDriveItemsAsync(IEnumerable<DriveItemRecord> items, CancellationToken ct)
    {
        // Batch apply inside a transaction to reduce contention on SQLite
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        foreach (var item in items)
        {
            var existing = await _db.DriveItems.FindAsync(new object[] { item.Id }, ct);
            if (existing is null) _db.DriveItems.Add(item);
            else _db.Entry(existing).CurrentValues.SetValues(item);
        }
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task<IEnumerable<DriveItemRecord>> GetPendingDownloadsAsync(int limit, CancellationToken ct) =>
        await _db.DriveItems.Where(d => !d.IsFolder && !d.IsDeleted)
                            .OrderBy(d => d.LastModifiedUtc)
                            .Take(limit)
                            .ToListAsync(ct);

    public async Task MarkLocalFileStateAsync(string driveItemId, SyncState state, CancellationToken ct)
    {
        // Use driveItemId as the local file id mapping for simplicity
        var drive = await _db.DriveItems.FindAsync(new object[] { driveItemId }, ct);
        if (drive is null) return;

        var local = await _db.LocalFiles.FindAsync(new object[] { driveItemId }, ct);
        if (local is null)
        {
            _db.LocalFiles.Add(new LocalFileRecord(driveItemId, drive.RelativePath, null, drive.Size, drive.LastModifiedUtc, state));
        }
        else
        {
            _db.Entry(local).CurrentValues.SetValues(local with { SyncState = state, LastWriteUtc = drive.LastModifiedUtc, Size = drive.Size });
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddOrUpdateLocalFileAsync(LocalFileRecord file, CancellationToken ct)
    {
        var existing = await _db.LocalFiles.FindAsync(new object[] { file.Id }, ct);
        if (existing is null) _db.LocalFiles.Add(file);
        else _db.Entry(existing).CurrentValues.SetValues(file);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<LocalFileRecord>> GetPendingUploadsAsync(int limit, CancellationToken ct) =>
        await _db.LocalFiles.Where(l => l.SyncState == SyncState.PendingUpload).Take(limit).ToListAsync(ct);

    public async Task LogTransferAsync(TransferLog log, CancellationToken ct)
    {
        _db.TransferLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }
}

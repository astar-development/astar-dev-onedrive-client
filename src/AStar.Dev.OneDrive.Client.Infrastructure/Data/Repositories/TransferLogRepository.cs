using AStar.Dev.OneDrive.Client.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Repositories;

/// <summary>
/// Repository for TransferLog entities with safe upsert logic
/// Not used at the moment; example of handling tracked vs detached entities
/// </summary>
public sealed class TransferLogRepository(DbContextOptions<AppDbContext> options)
{

    /// <summary>
    /// Upsert a TransferLog safely: avoids duplicate tracked instances.
    /// If an instance is already tracked, update it; otherwise load from DB or add.
    /// </summary>
    public async Task UpsertAsync(TransferLog detached)
    {
        await using var db = new AppDbContext(options);

        // 1) Check local ChangeTracker for an already tracked instance
        TransferLog? local = db.ChangeTracker.Entries<TransferLog>()
                      .FirstOrDefault(e => e.Entity.Id == detached.Id)?.Entity;

        if(local != null)
        {
            // Update tracked instance
            db.Entry(local).CurrentValues.SetValues(detached);
            _ = await db.SaveChangesAsync();
            return;
        }

        // 2) Try to load from DB (tracked after query)
        TransferLog? existing = await db.TransferLogs.FindAsync(detached.Id);
        if(existing != null)
        {
            db.Entry(existing).CurrentValues.SetValues(detached);
            _ = await db.SaveChangesAsync();
            return;
        }

        // 3) Not found — safe to add as new
        _ = db.TransferLogs.Add(detached);
        _ = await db.SaveChangesAsync();
    }

    /// <summary>Simple read helper</summary>
    public async Task<TransferLog?> GetByIdAsync(Guid id)
    {
        await using var db = new AppDbContext(options);
        return await db.TransferLogs.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id.ToString());
    }
}

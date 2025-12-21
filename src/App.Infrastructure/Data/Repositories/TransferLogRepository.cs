using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using App.Core.Entities;

namespace App.Infrastructure.Data.Repositories;

/// <summary>
/// Repository for TransferLog entities with safe upsert logic
/// Not used at the moment; example of handling tracked vs detached entities
/// </summary>
public sealed class TransferLogRepository
{
    private readonly DbContextOptions<AppDbContext> _options;

    public TransferLogRepository(DbContextOptions<AppDbContext> options) => _options = options;

    /// <summary>
    /// Upsert a TransferLog safely: avoids duplicate tracked instances.
    /// If an instance is already tracked, update it; otherwise load from DB or add.
    /// </summary>
    public async Task UpsertAsync(TransferLog detached)
    {
        await using var db = new AppDbContext(_options);

        // 1) Check local ChangeTracker for an already tracked instance
        var local = db.ChangeTracker.Entries<TransferLog>()
                      .FirstOrDefault(e => e.Entity.Id == detached.Id)?.Entity;

        if (local != null)
        {
            // Update tracked instance
            db.Entry(local).CurrentValues.SetValues(detached);
            await db.SaveChangesAsync();
            return;
        }

        // 2) Try to load from DB (tracked after query)
        var existing = await db.TransferLogs.FindAsync(detached.Id);
        if (existing != null)
        {
            db.Entry(existing).CurrentValues.SetValues(detached);
            await db.SaveChangesAsync();
            return;
        }

        // 3) Not found — safe to add as new
        db.TransferLogs.Add(detached);
        await db.SaveChangesAsync();
    }

    /// <summary>Simple read helper</summary>
    public async Task<TransferLog?> GetByIdAsync(Guid id)
    {
        await using var db = new AppDbContext(_options);
        return await db.TransferLogs.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id.ToString());
    }
}

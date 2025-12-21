using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Data;

public static class DbInitializer
{
    public static void EnsureDatabaseCreatedAndConfigured(AppDbContext db)
    {
        // Ensure DB created and enable WAL for better concurrency
        db.Database.OpenConnection();
        _ = db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
        db.Database.Migrate();
    }
}

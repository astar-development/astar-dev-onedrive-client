using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace App.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Look for a connection string in the repo root or fallback to a simple file-based DB
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables();

        IConfigurationRoot config = builder.Build();
        var conn = config.GetConnectionString("DefaultConnection") ?? "Data Source=/home/jason/.config/astar-dev/astar-dev-onedrive-client/database/app.db"; // FIX THIS

        var options = new DbContextOptionsBuilder<AppDbContext>();
        _ = options.UseSqlite(conn);

        return new AppDbContext(options.Options);
    }
}

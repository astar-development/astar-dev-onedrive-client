using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace App.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Look for a connection string in the repo root or fallback to a simple file-based DB
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables();

        var config = builder.Build();
        var conn = config.GetConnectionString("DefaultConnection") ?? "Data Source=app.db";

        var options = new DbContextOptionsBuilder<AppDbContext>();
        options.UseSqlite(conn);

        return new AppDbContext(options.Options);
    }
}

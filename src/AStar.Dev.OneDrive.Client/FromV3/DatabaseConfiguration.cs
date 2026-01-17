namespace AStar.Dev.OneDrive.Client.FromV3;

/// <summary>
/// Configuration for database connection settings.
/// </summary>
public sealed class DatabaseConfiguration
{
    /// <summary>
    /// Gets the database file path.
    /// </summary>
    public static string DatabasePath
    {
        get
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolder = Path.Combine(appDataPath, "AStar.Dev.OneDrive.Client");

            _ = Directory.CreateDirectory(appFolder);

            return Path.Combine(appFolder, "sync.db");
        }
    }

    /// <summary>
    /// Gets the SQLite connection string.
    /// </summary>
    public static string ConnectionString => $"Data Source={DatabasePath}";
}

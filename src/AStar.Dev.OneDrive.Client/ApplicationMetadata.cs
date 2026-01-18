
namespace AStar.Dev.OneDrive.Client;

public static class ApplicationMetadata
{
    /// <summary>
    /// The name of the application.
    /// </summary>
    public const string ApplicationName = "AStar Dev OneDrive Sync Client";
    public static readonly string ApplicationFolder = ApplicationName.Replace(" ", "-").ToLowerInvariant();

    /// <summary>
    /// The version of the application.
    /// </summary>
    public static readonly string ApplicationVersion = BuildApplicationVersion();

    private static string BuildApplicationVersion()
    {
        Version? version = typeof(ApplicationMetadata).Assembly.GetName().Version;
        return version is null ? "1.0.0-alpha" : $"{version.Major}.{version.Minor}.{version.Build}-alpha";
    }
}

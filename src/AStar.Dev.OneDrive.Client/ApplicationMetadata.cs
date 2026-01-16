namespace AStar.Dev.OneDrive.Client;

public static class ApplicationMetadata
{
    /// <summary>
    /// The name of the application.
    /// </summary>
    public const string ApplicationName = "AStar Dev OneDrive Sync Client";

    /// <summary>
    /// The version of the application.
    /// </summary>
    public static readonly string ApplicationVersion = BuildApplicationVersion();

    private static string BuildApplicationVersion()
    {
        var version = typeof(ApplicationMetadata).Assembly.GetName().Version;
        if(version is null)
            return "1.0.0-alpha";

        return $"{version.Major}.{version.Minor}.{version.Build}-alpha";
    } 
}

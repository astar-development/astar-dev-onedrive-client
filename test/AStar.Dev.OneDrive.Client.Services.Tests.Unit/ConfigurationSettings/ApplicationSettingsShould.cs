using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.Utilities;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit.ConfigurationSettings;

public class ApplicationSettingsShould
{
    [Fact]
    public void ContainExpectedPropertiesWithExpectedValues()
    {
        var settings = new ApplicationSettings
        {
            CachePrefix = "TestCachePrefix_",
            OneDriveRootDirectory = "TestOneDriveRootPath",
            UserPreferencesFile = "TestUserPreferencesFile",
            DatabaseName = "MockDatabaseName",
            UserPreferencesPath = "TestUserPreferencesPath",
            CacheTag = 42,
            ApplicationVersion = "1.2.3"
        };

        var result = settings.ToJson();

        result.ShouldMatchApproved();
    }

    [Fact]
    public void CombineUserPreferencesDirectoryAndFileNameForFullPath()
    {
        var settings = new ApplicationSettings { UserPreferencesFile = "test-preferences.json" };

        var result = settings.FullUserPreferencesPath;

        result.ShouldEndWith(Path.Combine("astar-dev-onedrive-client", "test-preferences.json"));
    }

    [Fact]
    public void UseDefaultUserPreferencesFileWhenNotSet()
    {
        var settings = new ApplicationSettings();

        var result = settings.FullUserPreferencesPath;

        result.ShouldEndWith(Path.Combine("astar-dev-onedrive-client", "user-preferences.json"));
    }

    [Fact]
    public void BuildFullUserPreferencesDirectoryFromAppDataPath()
    {
        var result = ApplicationSettings.FullUserPreferencesDirectory;

        result.ShouldEndWith("astar-dev-onedrive-client");
        result.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CombineDatabaseDirectoryAndFileNameForFullDatabasePath()
    {
        var settings = new ApplicationSettings { DatabaseName = "test-sync.db" };

        var result = settings.FullDatabasePath;

        result.ShouldEndWith(Path.Combine("astar-dev-onedrive-client", "database", "test-sync.db"));
    }

    [Fact]
    public void UseDefaultDatabaseNameWhenNotSet()
    {
        var settings = new ApplicationSettings();

        var result = settings.FullDatabasePath;

        result.ShouldEndWith(Path.Combine("astar-dev-onedrive-client", "database", "onedrive-sync.db"));
    }

    [Fact]
    public void BuildFullDatabaseDirectoryFromAppDataPath()
    {
        var result = ApplicationSettings.FullDatabaseDirectory;

        result.ShouldEndWith(Path.Combine("astar-dev-onedrive-client", "database"));
        result.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void CombineUserHomeFolderAndOneDriveRootForFullSyncPath()
    {
        var settings = new ApplicationSettings { OneDriveRootDirectory = "CustomSync" };

        var result = settings.FullUserSyncPath;

        result.ShouldEndWith("CustomSync");
        result.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public void UseDefaultOneDriveRootDirectoryWhenNotSet()
    {
        ApplicationSettings settings = new();

        var result = settings.FullUserSyncPath;

        result.ShouldEndWith("OneDrive-Sync");
    }

    [Fact]
    public void HaveExpectedDefaultValues()
    {
        ApplicationSettings settings = new();

        settings.CacheTag.ShouldBe(1);
        settings.ApplicationVersion.ShouldBe("1.0.0");
        settings.UserPreferencesPath.ShouldBe(string.Empty);
        settings.UserPreferencesFile.ShouldBe("user-preferences.json");
        settings.DatabaseName.ShouldBe("onedrive-sync.db");
        settings.OneDriveRootDirectory.ShouldBe("OneDrive-Sync");
        settings.CachePrefix.ShouldBe(string.Empty);
    }

    [Fact]
    public void AllowSettingAllProperties()
    {
        ApplicationSettings settings = new()
        {
            CacheTag = 42,
            ApplicationVersion = "2.0.0",
            UserPreferencesPath = "custom-path",
            UserPreferencesFile = "custom-prefs.json",
            DatabaseName = "custom-db.db",
            OneDriveRootDirectory = "CustomOneDrive",
            CachePrefix = "custom-prefix"
        };

        settings.CacheTag.ShouldBe(42);
        settings.ApplicationVersion.ShouldBe("2.0.0");
        settings.UserPreferencesPath.ShouldBe("custom-path");
        settings.UserPreferencesFile.ShouldBe("custom-prefs.json");
        settings.DatabaseName.ShouldBe("custom-db.db");
        settings.OneDriveRootDirectory.ShouldBe("CustomOneDrive");
        settings.CachePrefix.ShouldBe("custom-prefix");
    }
}

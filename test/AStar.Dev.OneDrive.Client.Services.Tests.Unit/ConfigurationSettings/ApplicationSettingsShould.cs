using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.Utilities;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit.ConfigurationSettings;

public class ApplicationSettingsShould
{
    [Fact]
    public void ContainTheExpectedPropertiesWithTheExpectedValues()
        => new ApplicationSettings() { CachePrefix = "TestCachePrefix_", OneDriveRootDirectory = "TestOneDriveRootPath", UserPreferencesFile = "TestUserPreferencesFile", DatabaseName = "MockDatabaseName", UserPreferencesPath = "TestUserPreferencesPath", CacheTag = 42, ApplicationVersion = "1.2.3" }
            .ToJson()
            .ShouldMatchApproved();
}

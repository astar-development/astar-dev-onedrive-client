using App.UI.Avalonia.ConfigurationSettings;
using AStar.Dev.Utilities;

namespace App.UI.Avalonia.Tests.Unit.ConfigurationSettings;

public class ApplicationSettingsShould
{
    [Fact]
    public void ContainTheExpectedPropertiesWithTheExpectedValues()
        => new ApplicationSettings() { CachePrefix = "TestCachePrefix_", OneDriveRootPath = "TestOneDriveRootPath", UserPreferencesFile = "TestUserPreferencesFile", UserPreferencesPath = "TestUserPreferencesPath", CacheTag = 42, ApplicationVersion = "1.2.3" }
            .ToJson()
            .ShouldMatchApproved();
}

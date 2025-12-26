using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit.ConfigurationSettings;

public class UserPreferencesShould
{
    [Fact]
    public void InitializeWithDefaultWindowSettings()
    {
        UserPreferences preferences = new();

        preferences.WindowSettings.ShouldNotBeNull();
        preferences.WindowSettings.WindowWidth.ShouldBe(1000);
        preferences.WindowSettings.WindowHeight.ShouldBe(800);
        preferences.WindowSettings.WindowX.ShouldBe(100);
        preferences.WindowSettings.WindowY.ShouldBe(100);
    }

    [Fact]
    public void InitializeWithDefaultUiSettings()
    {
        UserPreferences preferences = new();

        preferences.UiSettings.ShouldNotBeNull();
        preferences.UiSettings.RememberMe.ShouldBeTrue();
        preferences.UiSettings.Theme.ShouldBe("Auto");
        preferences.UiSettings.LastAction.ShouldBe("No action yet");
    }

    [Fact]
    public void UpdateFromAnotherUserPreferencesInstance()
    {
        UserPreferences preferences = new();
        UserPreferences other = new()
        {
            WindowSettings = new WindowSettings
            {
                WindowWidth = 1920,
                WindowHeight = 1080,
                WindowX = 50,
                WindowY = 75
            },
            UiSettings = new UiSettings
            {
                DownloadFilesAfterSync = true,
                RememberMe = false,
                Theme = "Dark",
                LastAction = "Sync completed"
            }
        };

        UserPreferences result = preferences.Update(other);

        result.WindowSettings.WindowWidth.ShouldBe(1920);
        result.WindowSettings.WindowHeight.ShouldBe(1080);
        result.WindowSettings.WindowX.ShouldBe(50);
        result.WindowSettings.WindowY.ShouldBe(75);
        result.UiSettings.DownloadFilesAfterSync.ShouldBeTrue();
        result.UiSettings.RememberMe.ShouldBeFalse();
        result.UiSettings.Theme.ShouldBe("Dark");
        result.UiSettings.LastAction.ShouldBe("Sync completed");
    }

    [Fact]
    public void ReturnSameInstanceAfterUpdate()
    {
        UserPreferences preferences = new();
        UserPreferences other = new();

        UserPreferences result = preferences.Update(other);

        result.ShouldBeSameAs(preferences);
    }

    [Fact]
    public void UpdateBothWindowAndUiSettingsSimultaneously()
    {
        UserPreferences preferences = new()
        {
            WindowSettings = new WindowSettings
            {
                WindowWidth = 800,
                WindowHeight = 600,
                WindowX = 0,
                WindowY = 0
            },
            UiSettings = new UiSettings
            {
                Theme = "Light",
                LastAction = "Initial"
            }
        };

        UserPreferences other = new()
        {
            WindowSettings = new WindowSettings
            {
                WindowWidth = 1280,
                WindowHeight = 720,
                WindowX = 100,
                WindowY = 200
            },
            UiSettings = new UiSettings
            {
                Theme = "Dark",
                LastAction = "Updated"
            }
        };

        preferences.Update(other);

        preferences.WindowSettings.WindowWidth.ShouldBe(1280);
        preferences.WindowSettings.WindowHeight.ShouldBe(720);
        preferences.UiSettings.Theme.ShouldBe("Dark");
        preferences.UiSettings.LastAction.ShouldBe("Updated");
    }

    [Fact]
    public void MaintainReferenceToOriginalSettingsObjectsAfterUpdate()
    {
        UserPreferences preferences = new();
        WindowSettings originalWindowSettings = preferences.WindowSettings;
        UiSettings originalUiSettings = preferences.UiSettings;

        UserPreferences other = new()
        {
            WindowSettings = new WindowSettings { WindowWidth = 1920 },
            UiSettings = new UiSettings { Theme = "Dark" }
        };

        preferences.Update(other);

        preferences.WindowSettings.ShouldBeSameAs(originalWindowSettings);
        preferences.UiSettings.ShouldBeSameAs(originalUiSettings);
    }
}

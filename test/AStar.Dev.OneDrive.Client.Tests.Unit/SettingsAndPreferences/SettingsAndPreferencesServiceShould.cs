using System.IO.Abstractions.TestingHelpers;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.SettingsAndPreferences;
using AStar.Dev.Utilities;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.SettingsAndPreferences;

public class SettingsAndPreferencesServiceShould
{
    [Fact]
    public void LoadUserPreferencesFromFile()
    {
        var fileSystem = new MockFileSystem();
        var appSettings = new ApplicationSettings { UserPreferencesPath = @"C:\TestPath", UserPreferencesFile = "prefs.json" };
        var expectedPreferences = new UserPreferences
        {
            WindowSettings = new WindowSettings { WindowWidth = 800, WindowHeight = 600 }
        };
        fileSystem.AddFile(appSettings.FullUserPreferencesPath, new MockFileData(expectedPreferences.ToJson()));
        var sut = new SettingsAndPreferencesService(fileSystem, appSettings);

        var result = sut.Load();

        result.WindowSettings.WindowWidth.ShouldBe(800);
        result.WindowSettings.WindowHeight.ShouldBe(600);
    }

    [Fact]
    public void SaveUserPreferencesToFile()
    {
        var fileSystem = new MockFileSystem();
        var appSettings = new ApplicationSettings { UserPreferencesPath = @"C:\TestPath", UserPreferencesFile = "prefs.json" };
        fileSystem.AddDirectory(ApplicationSettings.FullUserPreferencesDirectory);
        var sut = new SettingsAndPreferencesService(fileSystem, appSettings);
        var preferences = new UserPreferences
        {
            WindowSettings = new WindowSettings { WindowWidth = 1024, WindowHeight = 768 }
        };

        sut.Save(preferences);

        var savedContent = fileSystem.File.ReadAllText(appSettings.FullUserPreferencesPath);
        var savedPreferences = savedContent.FromJson<UserPreferences>();
        savedPreferences.WindowSettings.WindowWidth.ShouldBe(1024);
        savedPreferences.WindowSettings.WindowHeight.ShouldBe(768);
    }

    [Fact]
    public void ThrowExceptionWhenLoadingNonExistentFile()
    {
        var fileSystem = new MockFileSystem();
        var appSettings = new ApplicationSettings { UserPreferencesPath = @"C:\TestPath", UserPreferencesFile = "missing.json" };
        var sut = new SettingsAndPreferencesService(fileSystem, appSettings);

        var exception = Should.Throw<FileNotFoundException>(() => sut.Load());

        exception.ShouldNotBeNull();
    }

    [Fact]
    public void ThrowExceptionWhenLoadingInvalidJson()
    {
        var fileSystem = new MockFileSystem();
        var appSettings = new ApplicationSettings { UserPreferencesPath = @"C:\TestPath", UserPreferencesFile = "invalid.json" };
        fileSystem.AddFile(appSettings.FullUserPreferencesPath, new MockFileData("{ invalid json }"));
        var sut = new SettingsAndPreferencesService(fileSystem, appSettings);

        var exception = Should.Throw<System.Text.Json.JsonException>(() => sut.Load());

        exception.ShouldNotBeNull();
    }

    [Fact]
    public void PreserveAllUserPreferencesPropertiesWhenSavingAndLoading()
    {
        var fileSystem = new MockFileSystem();
        var appSettings = new ApplicationSettings { UserPreferencesPath = @"C:\TestPath", UserPreferencesFile = "prefs.json" };
        fileSystem.AddDirectory(ApplicationSettings.FullUserPreferencesDirectory);
        var sut = new SettingsAndPreferencesService(fileSystem, appSettings);
        var originalPreferences = new UserPreferences
        {
            WindowSettings = new WindowSettings
            {
                WindowWidth = 1920,
                WindowHeight = 1080,
                WindowX = 100,
                WindowY = 200
            },
            UiSettings = new UiSettings
            {
                Theme = "Dark",
                DownloadFilesAfterSync = true,
                RememberMe = true
            }
        };

        sut.Save(originalPreferences);
        var loadedPreferences = sut.Load();

        loadedPreferences.WindowSettings.WindowWidth.ShouldBe(1920);
        loadedPreferences.WindowSettings.WindowHeight.ShouldBe(1080);
        loadedPreferences.WindowSettings.WindowX.ShouldBe(100);
        loadedPreferences.WindowSettings.WindowY.ShouldBe(200);
        loadedPreferences.UiSettings.Theme.ShouldBe("Dark");
        loadedPreferences.UiSettings.DownloadFilesAfterSync.ShouldBeTrue();
        loadedPreferences.UiSettings.RememberMe.ShouldBeTrue();
    }

    [Fact]
    public void OverwriteExistingFileWhenSaving()
    {
        var fileSystem = new MockFileSystem();
        var appSettings = new ApplicationSettings { UserPreferencesPath = @"C:\TestPath", UserPreferencesFile = "prefs.json" };
        var existingPreferences = new UserPreferences
        {
            WindowSettings = new WindowSettings { WindowWidth = 800, WindowHeight = 600 }
        };
        fileSystem.AddFile(appSettings.FullUserPreferencesPath, new MockFileData(existingPreferences.ToJson()));
        var sut = new SettingsAndPreferencesService(fileSystem, appSettings);
        var newPreferences = new UserPreferences
        {
            WindowSettings = new WindowSettings { WindowWidth = 1024, WindowHeight = 768 }
        };

        sut.Save(newPreferences);
        var loadedPreferences = sut.Load();

        loadedPreferences.WindowSettings.WindowWidth.ShouldBe(1024);
        loadedPreferences.WindowSettings.WindowHeight.ShouldBe(768);
    }

    [Fact]
    public void HandleEmptyUserPreferencesObject()
    {
        var fileSystem = new MockFileSystem();
        var appSettings = new ApplicationSettings { UserPreferencesPath = @"C:\TestPath", UserPreferencesFile = "prefs.json" };
        fileSystem.AddDirectory(ApplicationSettings.FullUserPreferencesDirectory);
        var sut = new SettingsAndPreferencesService(fileSystem, appSettings);
        var emptyPreferences = new UserPreferences();

        sut.Save(emptyPreferences);
        var loadedPreferences = sut.Load();

        loadedPreferences.ShouldNotBeNull();
        loadedPreferences.WindowSettings.ShouldNotBeNull();
        loadedPreferences.UiSettings.ShouldNotBeNull();
    }
}

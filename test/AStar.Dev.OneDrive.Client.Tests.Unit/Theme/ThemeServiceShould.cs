using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.Theme;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Theme;

public class ThemeServiceShould
{
    [Theory]
    [InlineData("Light")]
    [InlineData("Dark")]
    [InlineData("Auto")]
    [InlineData("Unknown")]
    [InlineData("")]
    public void HandleVariousThemePreferencesWithoutError(string themeName)
    {
        var sut = new ThemeService();
        UserPreferences preferences = new() { UiSettings = new UiSettings { Theme = themeName } };

        Exception? exception = Record.Exception(() => sut.ApplyThemePreference(preferences));

        exception.ShouldBeNull();
    }

    [Fact]
    public void HandleDefaultUserPreferencesWithoutError()
    {
        var sut = new ThemeService();
        UserPreferences preferences = new();

        Exception? exception = Record.Exception(() => sut.ApplyThemePreference(preferences));

        exception.ShouldBeNull();
    }
}

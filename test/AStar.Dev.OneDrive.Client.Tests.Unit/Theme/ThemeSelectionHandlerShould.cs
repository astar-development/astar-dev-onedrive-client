using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.Theme;
using Avalonia.Controls;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Theme;

public class ThemeSelectionHandlerShould
{
    [Fact]
    public void UpdatePreferenceWhenIndex1Selected()
    {
        IThemeMapper mockMapper = Substitute.For<IThemeMapper>();
        IThemeService mockThemeService = Substitute.For<IThemeService>();
        mockMapper.MapIndexToTheme(1).Returns("Light");
        var sut = new ThemeSelectionHandler(mockMapper, mockThemeService);
        UserPreferences preferences = new();

        sut.UpdatePreferenceOnChange(1, preferences);

        preferences.UiSettings.Theme.ShouldBe("Light");
        mockThemeService.Received(1).ApplyThemePreference(preferences);
    }

    [Fact]
    public void UpdatePreferenceWhenIndex2Selected()
    {
        IThemeMapper mockMapper = Substitute.For<IThemeMapper>();
        IThemeService mockThemeService = Substitute.For<IThemeService>();
        mockMapper.MapIndexToTheme(2).Returns("Dark");
        var sut = new ThemeSelectionHandler(mockMapper, mockThemeService);
        UserPreferences preferences = new();

        sut.UpdatePreferenceOnChange(2, preferences);

        preferences.UiSettings.Theme.ShouldBe("Dark");
        mockThemeService.Received(1).ApplyThemePreference(preferences);
    }

    [Fact]
    public void UpdatePreferenceWhenIndex0Selected()
    {
        IThemeMapper mockMapper = Substitute.For<IThemeMapper>();
        IThemeService mockThemeService = Substitute.For<IThemeService>();
        mockMapper.MapIndexToTheme(0).Returns("Auto");
        var sut = new ThemeSelectionHandler(mockMapper, mockThemeService);
        UserPreferences preferences = new();

        sut.UpdatePreferenceOnChange(0, preferences);

        preferences.UiSettings.Theme.ShouldBe("Auto");
        mockThemeService.Received(1).ApplyThemePreference(preferences);
    }

    [Fact]
    public void CallMapperToConvertIndexToTheme()
    {
        IThemeMapper mockMapper = Substitute.For<IThemeMapper>();
        IThemeService mockThemeService = Substitute.For<IThemeService>();
        mockMapper.MapIndexToTheme(1).Returns("Light");
        var sut = new ThemeSelectionHandler(mockMapper, mockThemeService);
        UserPreferences preferences = new();

        sut.UpdatePreferenceOnChange(1, preferences);

        mockMapper.Received(1).MapIndexToTheme(1);
    }

    [Fact]
    public void ApplyThemeAfterUpdatingPreference()
    {
        IThemeMapper mockMapper = Substitute.For<IThemeMapper>();
        IThemeService mockThemeService = Substitute.For<IThemeService>();
        mockMapper.MapIndexToTheme(2).Returns("Dark");
        var sut = new ThemeSelectionHandler(mockMapper, mockThemeService);
        UserPreferences preferences = new();

        sut.UpdatePreferenceOnChange(2, preferences);

        mockThemeService.Received(1).ApplyThemePreference(Arg.Is<UserPreferences>(p => p.UiSettings.Theme == "Dark"));
    }

    [Fact]
    public void SetComboBoxSelectedIndexDuringInitialization()
    {
        IThemeMapper mockMapper = Substitute.For<IThemeMapper>();
        IThemeService mockThemeService = Substitute.For<IThemeService>();
        mockMapper.MapThemeToIndex("Dark").Returns(2);
        var sut = new ThemeSelectionHandler(mockMapper, mockThemeService);
        UserPreferences preferences = new() { UiSettings = new UiSettings { Theme = "Dark" } };
        ComboBox comboBox = new();

        sut.Initialize(comboBox, preferences);

        comboBox.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void ApplyThemePreferenceDuringInitialization()
    {
        IThemeMapper mockMapper = Substitute.For<IThemeMapper>();
        IThemeService mockThemeService = Substitute.For<IThemeService>();
        mockMapper.MapThemeToIndex("Light").Returns(1);
        var sut = new ThemeSelectionHandler(mockMapper, mockThemeService);
        UserPreferences preferences = new() { UiSettings = new UiSettings { Theme = "Light" } };
        ComboBox comboBox = new();

        sut.Initialize(comboBox, preferences);

        mockThemeService.Received(1).ApplyThemePreference(preferences);
    }

    [Fact]
    public void CallMapperToConvertThemeToIndexDuringInitialization()
    {
        IThemeMapper mockMapper = Substitute.For<IThemeMapper>();
        IThemeService mockThemeService = Substitute.For<IThemeService>();
        mockMapper.MapThemeToIndex("Auto").Returns(0);
        var sut = new ThemeSelectionHandler(mockMapper, mockThemeService);
        UserPreferences preferences = new() { UiSettings = new UiSettings { Theme = "Auto" } };
        ComboBox comboBox = new();

        sut.Initialize(comboBox, preferences);

        mockMapper.Received(1).MapThemeToIndex("Auto");
    }

    [Fact]
    public void HandleNegativeIndexGracefully()
    {
        IThemeMapper mockMapper = Substitute.For<IThemeMapper>();
        IThemeService mockThemeService = Substitute.For<IThemeService>();
        mockMapper.MapIndexToTheme(-1).Returns("Auto");
        var sut = new ThemeSelectionHandler(mockMapper, mockThemeService);
        UserPreferences preferences = new();

        Exception? exception = Record.Exception(() => sut.UpdatePreferenceOnChange(-1, preferences));

        exception.ShouldBeNull();
    }

    [Fact]
    public void HandleLargeIndexGracefully()
    {
        IThemeMapper mockMapper = Substitute.For<IThemeMapper>();
        IThemeService mockThemeService = Substitute.For<IThemeService>();
        mockMapper.MapIndexToTheme(999).Returns("Auto");
        var sut = new ThemeSelectionHandler(mockMapper, mockThemeService);
        UserPreferences preferences = new();

        Exception? exception = Record.Exception(() => sut.UpdatePreferenceOnChange(999, preferences));

        exception.ShouldBeNull();
    }
}

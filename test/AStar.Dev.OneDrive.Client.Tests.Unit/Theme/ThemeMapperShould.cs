using AStar.Dev.OneDrive.Client.Theme;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Theme;

public class ThemeMapperShould
{
    [Fact]
    public void MapLightThemeToIndex1()
    {
        var sut = new ThemeMapper();

        var result = sut.MapThemeToIndex("Light");

        result.ShouldBe(1);
    }

    [Fact]
    public void MapDarkThemeToIndex2()
    {
        var sut = new ThemeMapper();

        var result = sut.MapThemeToIndex("Dark");

        result.ShouldBe(2);
    }

    [Fact]
    public void MapAutoThemeToIndex0()
    {
        var sut = new ThemeMapper();

        var result = sut.MapThemeToIndex("Auto");

        result.ShouldBe(0);
    }

    [Fact]
    public void MapUnknownThemeToIndex0()
    {
        var sut = new ThemeMapper();

        var result = sut.MapThemeToIndex("Unknown");

        result.ShouldBe(0);
    }

    [Fact]
    public void MapEmptyStringThemeToIndex0()
    {
        var sut = new ThemeMapper();

        var result = sut.MapThemeToIndex(string.Empty);

        result.ShouldBe(0);
    }

    [Fact]
    public void MapIndex1ToLightTheme()
    {
        var sut = new ThemeMapper();

        var result = sut.MapIndexToTheme(1);

        result.ShouldBe("Light");
    }

    [Fact]
    public void MapIndex2ToDarkTheme()
    {
        var sut = new ThemeMapper();

        var result = sut.MapIndexToTheme(2);

        result.ShouldBe("Dark");
    }

    [Fact]
    public void MapIndex0ToAutoTheme()
    {
        var sut = new ThemeMapper();

        var result = sut.MapIndexToTheme(0);

        result.ShouldBe("Auto");
    }

    [Fact]
    public void MapNegativeIndexToAutoTheme()
    {
        var sut = new ThemeMapper();

        var result = sut.MapIndexToTheme(-1);

        result.ShouldBe("Auto");
    }

    [Fact]
    public void MapLargeIndexToAutoTheme()
    {
        var sut = new ThemeMapper();

        var result = sut.MapIndexToTheme(999);

        result.ShouldBe("Auto");
    }

    [Fact]
    public void ProvideRoundTripConversionForLightTheme()
    {
        var sut = new ThemeMapper();

        var index = sut.MapThemeToIndex("Light");
        var theme = sut.MapIndexToTheme(index);

        theme.ShouldBe("Light");
    }

    [Fact]
    public void ProvideRoundTripConversionForDarkTheme()
    {
        var sut = new ThemeMapper();

        var index = sut.MapThemeToIndex("Dark");
        var theme = sut.MapIndexToTheme(index);

        theme.ShouldBe("Dark");
    }

    [Fact]
    public void ProvideRoundTripConversionForAutoTheme()
    {
        var sut = new ThemeMapper();

        var index = sut.MapThemeToIndex("Auto");
        var theme = sut.MapIndexToTheme(index);

        theme.ShouldBe("Auto");
    }

    [Theory]
    [InlineData("Light", 1)]
    [InlineData("Dark", 2)]
    [InlineData("Auto", 0)]
    [InlineData("light", 0)]
    [InlineData("DARK", 0)]
    public void MapThemeToIndexWithVariousCases(string themeName, int expectedIndex)
    {
        var sut = new ThemeMapper();

        var result = sut.MapThemeToIndex(themeName);

        result.ShouldBe(expectedIndex);
    }
}

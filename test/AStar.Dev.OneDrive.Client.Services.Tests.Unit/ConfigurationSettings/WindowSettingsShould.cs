using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using Avalonia;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDrive.Client.Services.Tests.Unit.ConfigurationSettings;

public class WindowSettingsShould
{
    [Fact]
    public void HaveExpectedDefaultValues()
    {
        WindowSettings settings = new();

        settings.WindowWidth.ShouldBe(1000);
        settings.WindowHeight.ShouldBe(800);
        settings.WindowX.ShouldBe(100);
        settings.WindowY.ShouldBe(100);
    }

    [Fact]
    public void UpdateFromAnotherWindowSettingsInstance()
    {
        WindowSettings settings = new();
        WindowSettings other = new()
        {
            WindowWidth = 1920,
            WindowHeight = 1080,
            WindowX = 50,
            WindowY = 75
        };

        WindowSettings result = settings.Update(other);

        result.WindowWidth.ShouldBe(1920);
        result.WindowHeight.ShouldBe(1080);
        result.WindowX.ShouldBe(50);
        result.WindowY.ShouldBe(75);
    }

    [Fact]
    public void ReturnSameInstanceAfterUpdateFromWindowSettings()
    {
        WindowSettings settings = new();
        WindowSettings other = new();

        WindowSettings result = settings.Update(other);

        result.ShouldBeSameAs(settings);
    }

    [Fact]
    public void UpdateFromPixelPointAndDimensions()
    {
        WindowSettings settings = new();
        PixelPoint position = new(200, 300);

        WindowSettings result = settings.Update(position, 1280, 720);

        result.WindowWidth.ShouldBe(1280);
        result.WindowHeight.ShouldBe(720);
        result.WindowX.ShouldBe(200);
        result.WindowY.ShouldBe(300);
    }

    [Fact]
    public void ReturnSameInstanceAfterUpdateFromPixelPoint()
    {
        WindowSettings settings = new();
        PixelPoint position = new(100, 100);

        WindowSettings result = settings.Update(position, 800, 600);

        result.ShouldBeSameAs(settings);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(-100, -200, 800, 600)]
    [InlineData(int.MaxValue, int.MaxValue, double.MaxValue, double.MaxValue)]
    public void AcceptAnyValuesInUpdate(int x, int y, double width, double height)
    {
        WindowSettings settings = new();
        PixelPoint position = new(x, y);

        WindowSettings result = settings.Update(position, width, height);

        result.WindowX.ShouldBe(x);
        result.WindowY.ShouldBe(y);
        result.WindowWidth.ShouldBe(width);
        result.WindowHeight.ShouldBe(height);
    }
}

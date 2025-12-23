using AStar.Dev.OneDrive.Client.ViewModels;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.ViewModels;

public class WindowPositionValidatorShould
{
    private readonly WindowPositionValidator _sut;

    public WindowPositionValidatorShould() => _sut = new WindowPositionValidator();

    [Theory]
    [InlineData(800, 600)]
    [InlineData(1920, 1080)]
    [InlineData(0.1, 0.1)]
    [InlineData(1, 1)]
    [InlineData(double.MaxValue, double.MaxValue)]
    public void ReturnTrueForValidSize(double width, double height)
    {
        var result = _sut.IsValidSize(width, height);

        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0, 600)]
    [InlineData(800, 0)]
    [InlineData(0, 0)]
    [InlineData(-1, 600)]
    [InlineData(800, -1)]
    [InlineData(-800, -600)]
    public void ReturnFalseForInvalidSize(double width, double height)
    {
        var result = _sut.IsValidSize(width, height);

        result.ShouldBeFalse();
    }

    [Theory]
    [InlineData(100, 200)]
    [InlineData(1920, 1080)]
    [InlineData(1, 1)]
    [InlineData(int.MaxValue, int.MaxValue)]
    public void ReturnTrueForValidPosition(int x, int y)
    {
        var result = _sut.IsValidPosition(x, y);

        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0, 100)]
    [InlineData(100, 0)]
    [InlineData(0, 0)]
    [InlineData(-1, 100)]
    [InlineData(100, -1)]
    [InlineData(-100, -200)]
    [InlineData(int.MinValue, int.MinValue)]
    public void ReturnFalseForInvalidPosition(int x, int y)
    {
        var result = _sut.IsValidPosition(x, y);

        result.ShouldBeFalse();
    }
}

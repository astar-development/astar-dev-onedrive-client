namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class OptionToResultTests
{
    [Fact]
    public void ToResultFromSomeReturnsOk()
    {
        var opt = new Option<int>.Some(42);
        var result = opt.ToResult(() => "missing");
        (result is Result<int, string>.Ok { Value: 42 }).ShouldBeTrue();
    }

    [Fact]
    public void ToResultFromNoneReturnsError()
    {
        var opt = Option.None<int>();
        var result = opt.ToResult(() => "missing");
        (result is Result<int, string>.Error { Reason: "missing" }).ShouldBeTrue();
    }
}

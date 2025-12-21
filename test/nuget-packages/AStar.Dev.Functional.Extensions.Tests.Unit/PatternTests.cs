namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class PatternTests
{
    [Fact]
    public void IsSomeAndIsNoneWorkCorrectly()
    {
        Option<string> some = new Option<string>.Some("value");
        var none = Option.None<string>();

        Assert.True(Pattern.IsSome(some));
        Assert.False(Pattern.IsNone(some));

        Assert.True(Pattern.IsNone(none));
        Assert.False(Pattern.IsSome(none));
    }

    [Fact]
    public void IsOkAndIsErrorWorkCorrectly()
    {
        Result<int, string> ok = new Result<int, string>.Ok(1);
        Result<int, string> err = new Result<int, string>.Error("fail");

        Assert.True(Pattern.IsOk(ok));
        Assert.False(Pattern.IsError(ok));

        Assert.True(Pattern.IsError(err));
        Assert.False(Pattern.IsOk(err));
    }

    [Fact]
    public void IsSuccessAndIsFailureWorkCorrectly()
    {
        Result<int, Exception> success = Try.Run(() => 1);
        Result<int, Exception> failure = Try.Run<int>(() => throw new ArgumentNullException("fail"));

        Assert.True(Pattern.IsSuccess(success));
        Assert.False(Pattern.IsFailure(success));

        Assert.True(Pattern.IsFailure(failure));
        Assert.False(Pattern.IsSuccess(failure));
    }
}

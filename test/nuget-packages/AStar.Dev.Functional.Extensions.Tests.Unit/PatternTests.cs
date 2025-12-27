namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class PatternTests
{
    [Fact]
    public void IsSomeAndIsNoneWorkCorrectly()
    {
        Option<string> some = new Option<string>.Some("value");
        var none = Option.None<string>();

        Pattern.IsSome(some).ShouldBeTrue();
        Pattern.IsNone(some).ShouldBeFalse();

        Pattern.IsNone(none).ShouldBeTrue();
        Pattern.IsSome(none).ShouldBeFalse();
    }

    [Fact]
    public void IsOkAndIsErrorWorkCorrectly()
    {
        Result<int, string> ok = new Result<int, string>.Ok(1);
        Result<int, string> err = new Result<int, string>.Error("fail");

        Pattern.IsOk(ok).ShouldBeTrue();
        Pattern.IsError(ok).ShouldBeFalse();

        Pattern.IsError(err).ShouldBeTrue();
        Pattern.IsOk(err).ShouldBeFalse();
    }

    [Fact]
    public void IsSuccessAndIsFailureWorkCorrectly()
    {
        Result<int, Exception> success = Try.Run(() => 1);
        Result<int, Exception> failure = Try.Run<int>(() => throw new ArgumentNullException("fail"));

        Pattern.IsSuccess(success).ShouldBeTrue();
        Pattern.IsFailure(success).ShouldBeFalse();

        Pattern.IsFailure(failure).ShouldBeTrue();
        Pattern.IsSuccess(failure).ShouldBeFalse();
    }
}

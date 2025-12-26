namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class TryTests
{
    [Fact]
    public void RunShouldReturnOkResultWhenFunctionSucceeds()
    {
        const int expectedValue = 42;

        Result<int, Exception> result = Try.Run(SuccessFunc);

        _ = result.ShouldBeOfType<Result<int, Exception>.Ok>();
        Pattern.IsOk(result).ShouldBeTrue();
        Pattern.IsError(result).ShouldBeFalse();
        ((Result<int, Exception>.Ok)result).Value.ShouldBe(expectedValue);

        return;

        static int SuccessFunc()
        {
            return expectedValue;
        }
    }

    [Fact]
    public void RunActionShouldReturnOkTrueWhenActionSucceeds()
    {
        
        Result<bool, Exception> result = Try.Run(() =>
        {
            /* no-op */
        });

        _ = result.ShouldBeOfType<Result<bool, Exception>.Ok>();
        Pattern.IsOk(result).ShouldBeTrue();
        Pattern.IsError(result).ShouldBeFalse();
        ((Result<bool, Exception>.Ok)result).Value.ShouldBeTrue();
    }

    [Fact]
    public void RunActionShouldReturnErrorWhenActionThrows()
    {

        var expected = new InvalidOperationException("sync boom");

        Result<bool, Exception> result = Try.Run(() => throw expected);

        _ = result.ShouldBeOfType<Result<bool, Exception>.Error>();
        Pattern.IsOk(result).ShouldBeFalse();
        Pattern.IsError(result).ShouldBeTrue();
        Exception reason = ((Result<bool, Exception>.Error)result).Reason;
        reason.ShouldBeSameAs(expected);
        reason.Message.ShouldBe("sync boom");
    }

    [Fact]
    public async Task RunAsyncActionShouldReturnOkTrueWhenActionSucceedsAsync()
    {
        
        Result<bool, Exception> result = await Try.RunAsync(async () => await Task.Delay(1));

        _ = result.ShouldBeOfType<Result<bool, Exception>.Ok>();
        Pattern.IsOk(result).ShouldBeTrue();
        Pattern.IsError(result).ShouldBeFalse();
        ((Result<bool, Exception>.Ok)result).Value.ShouldBeTrue();
    }

    [Fact]
    public async Task RunAsyncActionShouldReturnErrorWhenActionThrowsAsync()
    {

        var expected = new InvalidOperationException("async boom");

        Result<bool, Exception> result = await Try.RunAsync(async () =>
        {
            await Task.Yield();
            throw expected;
        });

        _ = result.ShouldBeOfType<Result<bool, Exception>.Error>();
        Pattern.IsOk(result).ShouldBeFalse();
        Pattern.IsError(result).ShouldBeTrue();
        Exception reason = ((Result<bool, Exception>.Error)result).Reason;
        reason.ShouldBeSameAs(expected);
        reason.Message.ShouldBe("async boom");
    }

    [Fact]
    public void RunShouldReturnErrorResultWhenFunctionThrows()
    {
        const string exceptionMessage = "Test exception";
        Exception expectedException = new InvalidOperationException(exceptionMessage);

        Result<int, Exception> result = Try.Run(FailingFunc);

        _ = result.ShouldBeOfType<Result<int, Exception>.Error>();
        Pattern.IsOk(result).ShouldBeFalse();
        Pattern.IsError(result).ShouldBeTrue();
        ((Result<int, Exception>.Error)result).Reason.ShouldBe(expectedException);
        ((Result<int, Exception>.Error)result).Reason.Message.ShouldBe(exceptionMessage);

        return;

        int FailingFunc()
        {
            throw expectedException;
        }
    }

    [Fact]
    public void RunShouldCaptureSpecificExceptionTypes()
    {
        Result<int, Exception> result = Try.Run(ArgNullFunc);

        Exception error = ((Result<int, Exception>.Error)result).Reason;
        _ = error.ShouldBeOfType<ArgumentNullException>();
        (error as ArgumentNullException)?.ParamName.ShouldBe("testParam");

        return;

        static int ArgNullFunc()
        {
            throw new ArgumentNullException("testParam");
        }
    }

    [Fact]
    public async Task RunAsyncShouldReturnOkResultWhenAsyncFunctionSucceedsAsync()
    {
        const string expectedValue = "async result";

        Result<string, Exception> result = await Try.RunAsync(SuccessFunc);

        _ = result.ShouldBeOfType<Result<string, Exception>.Ok>();
        Pattern.IsOk(result).ShouldBeTrue();
        Pattern.IsError(result).ShouldBeFalse();
        ((Result<string, Exception>.Ok)result).Value.ShouldBe(expectedValue);

        return;

        static Task<string> SuccessFunc()
        {
            return Task.FromResult(expectedValue);
        }
    }

    [Fact]
    public async Task RunAsyncShouldReturnErrorResultWhenAsyncFunctionThrowsAsync()
    {
        const string exceptionMessage = "Async test exception";
        Exception expectedException = new InvalidOperationException(exceptionMessage);

        Result<string, Exception> result = await Try.RunAsync(FailingFunc);

        _ = result.ShouldBeOfType<Result<string, Exception>.Error>();
        Pattern.IsOk(result).ShouldBeFalse();
        Pattern.IsError(result).ShouldBeTrue();
        ((Result<string, Exception>.Error)result).Reason.ShouldBe(expectedException);
        ((Result<string, Exception>.Error)result).Reason.Message.ShouldBe(exceptionMessage);

        return;

        Task<string> FailingFunc()
        {
            return Task.FromException<string>(expectedException);
        }
    }

    [Fact]
    public async Task RunAsyncShouldCaptureExceptionFromAsyncAwaitOperationAsync()
    {
        Result<int, Exception> result = await Try.RunAsync(FailingAsyncFunc);

        Pattern.IsError(result).ShouldBeTrue();
        _ = ((Result<int, Exception>.Error)result).Reason.ShouldBeOfType<TimeoutException>();
        ((Result<int, Exception>.Error)result).Reason.Message.ShouldBe("Operation timed out");

        return;

        static async Task<int> FailingAsyncFunc()
        {
            await Task.Delay(1);

            throw new TimeoutException("Operation timed out");
        }
    }

    [Fact]
    public void RunShouldWorkWithLambdaExpressions()
    {
        Result<int, Exception> result = Try.Run(() => 5 + 5);

        Pattern.IsOk(result).ShouldBeTrue();
        ((Result<int, Exception>.Ok)result).Value.ShouldBe(10);
    }

    [Fact]
    public async Task RunAsyncShouldWorkWithAsyncLambdaExpressionsAsync()
    {
        Result<string, Exception> result = await Try.RunAsync(async () =>
        {
            await Task.Delay(1);

            return "completed";
        });

        Pattern.IsOk(result).ShouldBeTrue();
        ((Result<string, Exception>.Ok)result).Value.ShouldBe("completed");
    }

    [Fact]
    public void RunShouldPreserveOriginalExceptionWithoutWrapping()
    {
        var customException = new CustomTestException("Custom exception test");

        Result<int, Exception> result = Try.Run<int>(() => throw customException);

        Exception error = ((Result<int, Exception>.Error)result).Reason;
        _ = error.ShouldBeOfType<CustomTestException>();
        error.ShouldBeSameAs(customException);
    }

    [Fact]
    public async Task RunAsyncShouldPreserveOriginalExceptionWithoutWrappingAsync()
    {
        var customException = new CustomTestException("Async custom exception test");

        Result<string, Exception> result = await Try.RunAsync<string>(async () =>
        {
            await Task.Delay(1);

            throw customException;
        });

        Exception error = ((Result<string, Exception>.Error)result).Reason;
        _ = error.ShouldBeOfType<CustomTestException>();
        error.ShouldBeSameAs(customException);
    }

    [Fact]
    public void TryRunCapturesSuccess()
    {
        Result<int, Exception> result = Try.Run(() => 42);

        var output = result.Match(
            ok => ok,
            ex => -1);

        Assert.Equal(42, output);
    }

    [Fact]
    public void TryRunCapturesException()
    {
        Result<int, Exception> result = Try.Run<int>(() => throw new InvalidOperationException("fail"));

        var output = result.Match(
            ok => ok,
            ex => -1);

        Assert.Equal(-1, output);
    }

    [Fact]
    public void TryMatchReturnsCorrectBranch()
    {
        Result<string, Exception> success = Try.Run(() => "done");
        Result<string, Exception> failure = Try.Run<string>(() => throw new InvalidOperationException("fail"));

        var a = success.Match(x => $"OK: {x}", ex => $"ERR: {ex.Message}");
        var b = failure.Match(x => $"OK: {x}", ex => $"ERR: {ex.Message}");

        Assert.Equal("OK: done", a);
        Assert.Equal("ERR: fail", b);
    }
}

namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class ResultExtensionMatchAsyncShould
{
    [Fact]
    public async Task MatchAsyncOnTaskResultCallSuccessFunctionWhenOk()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        var matched = await resultTask.MatchAsync(
            success => $"Success: {success}",
            error => $"Error: {error}");

        matched.ShouldBe("Success: 42");
    }

    [Fact]
    public async Task MatchAsyncOnTaskResultCallErrorFunctionWhenError()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("failure"));

        var matched = await resultTask.MatchAsync(
            success => $"Success: {success}",
            error => $"Error: {error}");

        matched.ShouldBe("Error: failure");
    }

    [Fact]
    public async Task MatchAsyncOnTaskResultWithAsyncHandlersCallSuccessFunctionWhenOk()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        var matched = await resultTask.MatchAsync(
            success => Task.FromResult($"Success: {success}"),
            error => Task.FromResult($"Error: {error}"));

        matched.ShouldBe("Success: 42");
    }

    [Fact]
    public async Task MatchAsyncOnTaskResultWithAsyncHandlersCallErrorFunctionWhenError()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("failure"));

        var matched = await resultTask.MatchAsync(
            success => Task.FromResult($"Success: {success}"),
            error => Task.FromResult($"Error: {error}"));

        matched.ShouldBe("Error: failure");
    }

    [Fact]
    public async Task MatchAsyncOnTaskResultReturningResultCallSuccessFunctionWhenOk()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        Result<string, Exception> matched = await resultTask.MatchAsync(
            success => new Result<string, Exception>.Ok($"Success: {success}"),
            error => new Result<string, Exception>.Error(new InvalidOperationException(error)));

        _ = matched.ShouldBeOfType<Result<string, Exception>.Ok>();
        ((Result<string, Exception>.Ok)matched).Value.ShouldBe("Success: 42");
    }

    [Fact]
    public async Task MatchAsyncOnTaskResultReturningResultCallErrorFunctionWhenError()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("failure"));

        Result<string, Exception> matched = await resultTask.MatchAsync(
            success => new Result<string, Exception>.Ok($"Success: {success}"),
            error => new Result<string, Exception>.Error(new InvalidOperationException(error)));

        _ = matched.ShouldBeOfType<Result<string, Exception>.Error>();
        ((Result<string, Exception>.Error)matched).Reason.Message.ShouldBe("failure");
    }

    [Fact]
    public async Task MatchAsyncOnTaskResultReturningResultTransformsBothCases()
    {
        Task<Result<bool, Exception>> okTask = Task.FromResult<Result<bool, Exception>>(
            new Result<bool, Exception>.Ok(true));
        Task<Result<bool, Exception>> errorTask = Task.FromResult<Result<bool, Exception>>(
            new Result<bool, Exception>.Error(new InvalidOperationException("test error")));

        Result<int, string> okResult = await okTask.MatchAsync(
            success => new Result<int, string>.Ok(success ? 1 : 0),
            error => new Result<int, string>.Error(error.Message));

        Result<int, string> errorResult = await errorTask.MatchAsync(
            success => new Result<int, string>.Ok(success ? 1 : 0),
            error => new Result<int, string>.Error(error.Message));

        _ = okResult.ShouldBeOfType<Result<int, string>.Ok>();
        ((Result<int, string>.Ok)okResult).Value.ShouldBe(1);

        _ = errorResult.ShouldBeOfType<Result<int, string>.Error>();
        ((Result<int, string>.Error)errorResult).Reason.ShouldBe("test error");
    }

    [Fact]
    public async Task MatchAsyncOnTaskResultWithBlockBodiedLambdasInfersTypesCorrectly()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        Result<string, Exception> matched = await resultTask.MatchAsync(
            value =>
            {
                var message = $"Success: {value}";
                return new Result<string, Exception>.Ok(message);
            },
            error =>
            {
                var exception = new InvalidOperationException(error);
                return new Result<string, Exception>.Error(exception);
            });

        _ = matched.ShouldBeOfType<Result<string, Exception>.Ok>();
        ((Result<string, Exception>.Ok)matched).Value.ShouldBe("Success: 42");
    }
}

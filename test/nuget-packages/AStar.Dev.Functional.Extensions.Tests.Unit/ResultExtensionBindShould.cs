namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class ResultExtensionBindShould
{
    [Fact]
    public void BindSuccessValueToNewResultWhenResultIsOk()
    {
        var result = new Result<int, string>.Ok(42);

        Result<string, string> bound = result.Bind(value => new Result<string, string>.Ok(value.ToString()));

        _ = bound.ShouldBeOfType<Result<string, string>.Ok>();

        var matchResult = bound.Match(
            ok => ok,
            _ => throw new InvalidOperationException("Should not be error")
        );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public void BindSuccessValueToErrorResultWhenBindFunctionReturnsError()
    {
        var result = new Result<int, string>.Ok(42);

        Result<string, string> bound = result.Bind<int, string, string>(value => new Result<string, string>.Error("bound error"));

        _ = bound.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = bound.Match(
            _ => throw new InvalidOperationException("Should not be success"),
            err => err
        );

        matchResult.ShouldBe("bound error");
    }

    [Fact]
    public void PreserveErrorWhenBindingFailedResult()
    {
        var result = new Result<int, string>.Error("original error");

        Result<string, string> bound = result.Bind<int, string, string>(value => new Result<string, string>.Ok(value.ToString()));

        _ = bound.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = bound.Match(
            _ => throw new InvalidOperationException("Should not be success"),
            err => err
        );

        matchResult.ShouldBe("original error");
    }

    [Fact]
    public async Task BindSuccessValueAsyncToNewResultWhenResultIsOkAsync()
    {
        var result = new Result<int, string>.Ok(42);

        Result<string, string> bound = await result.BindAsync(value => Task.FromResult<Result<string, string>>(new Result<string, string>.Ok(value.ToString())));

        _ = bound.ShouldBeOfType<Result<string, string>.Ok>();

        var matchResult = bound.Match(
            ok => ok,
            _ => throw new InvalidOperationException("Should not be error")
        );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task BindSuccessValueAsyncToErrorResultWhenBindFunctionReturnsErrorAsync()
    {
        var result = new Result<int, string>.Ok(42);

        Result<string, string> bound = await result.BindAsync(value => Task.FromResult<Result<string, string>>(new Result<string, string>.Error("bound error")));

        _ = bound.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = bound.Match(
            _ => throw new InvalidOperationException("Should not be success"),
            err => err
        );

        matchResult.ShouldBe("bound error");
    }

    [Fact]
    public async Task PreserveErrorWhenBindingAsyncFailedResultAsync()
    {
        var result = new Result<int, string>.Error("original error");

        Result<string, string> bound = await result.BindAsync(value => Task.FromResult<Result<string, string>>(new Result<string, string>.Ok(value.ToString())));

        _ = bound.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = bound.Match(
            _ => throw new InvalidOperationException("Should not be success"),
            err => err
        );

        matchResult.ShouldBe("original error");
    }

    [Fact]
    public async Task BindSuccessValueFromTaskResultWhenResultIsOkAsync()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        Result<string, string> bound = await resultTask.BindAsync(value => new Result<string, string>.Ok(value.ToString()));

        _ = bound.ShouldBeOfType<Result<string, string>.Ok>();

        var matchResult = bound.Match(
            ok => ok,
            _ => throw new InvalidOperationException("Should not be error")
        );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task BindSuccessValueFromTaskResultToErrorWhenBindFunctionReturnsErrorAsync()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        Result<string, string> bound = await resultTask.BindAsync(value => new Result<string, string>.Error("bound error"));

        _ = bound.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = bound.Match(
            _ => throw new InvalidOperationException("Should not be success"),
            err => err
        );

        matchResult.ShouldBe("bound error");
    }

    [Fact]
    public async Task PreserveErrorWhenBindingFailedTaskResultAsync()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("original error"));

        Result<string, string> bound = await resultTask.BindAsync(value => new Result<string, string>.Ok(value.ToString()));

        _ = bound.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = bound.Match(
            _ => throw new InvalidOperationException("Should not be success"),
            err => err
        );

        matchResult.ShouldBe("original error");
    }

    [Fact]
    public async Task BindSuccessValueFromTaskResultAsyncWhenResultIsOkAsync()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        Result<string, string> bound = await resultTask.BindAsync(value => Task.FromResult<Result<string, string>>(new Result<string, string>.Ok(value.ToString())));

        _ = bound.ShouldBeOfType<Result<string, string>.Ok>();

        var matchResult = bound.Match(
            ok => ok,
            _ => throw new InvalidOperationException("Should not be error")
        );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task BindSuccessValueFromTaskResultAsyncToErrorWhenBindFunctionReturnsErrorAsync()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        Result<string, string> bound = await resultTask.BindAsync(value => Task.FromResult<Result<string, string>>(new Result<string, string>.Error("bound error")));

        _ = bound.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = bound.Match(
            _ => throw new InvalidOperationException("Should not be success"),
            err => err
        );

        matchResult.ShouldBe("bound error");
    }
}

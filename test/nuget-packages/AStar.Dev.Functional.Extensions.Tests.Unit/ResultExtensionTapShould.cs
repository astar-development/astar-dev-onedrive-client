namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class ResultExtensionTapShould
{
    [Fact]
    public void ExecuteSideEffectAndReturnOriginalResultWhenTappingSuccessResult()
    {
        var result = new Result<int, string>.Ok(42);
        var sideEffectValue = 0;

        Result<int, string> tapped = result.Tap(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public void NotExecuteSideEffectAndReturnOriginalResultWhenTappingErrorResult()
    {
        var result = new Result<int, string>.Error("error");
        var sideEffectValue = 0;

        Result<int, string> tapped = result.Tap(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public void ExecuteSideEffectAndReturnOriginalResultWhenTappingErrorOnErrorResult()
    {
        var result = new Result<string, int>.Error(42);
        var sideEffectValue = 0;

        Result<string, int> tapped = result.TapError(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public void NotExecuteSideEffectAndReturnOriginalResultWhenTappingErrorOnSuccessResult()
    {
        var result = new Result<string, int>.Ok("success");
        var sideEffectValue = 0;

        Result<string, int> tapped = result.TapError(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public async Task ExecuteAsyncSideEffectAndReturnOriginalResultWhenTappingSuccessResultAsync()
    {
        var result = new Result<int, string>.Ok(42);
        var sideEffectValue = 0;

        Result<int, string> tapped = await result.TapAsync(value =>
        {
            sideEffectValue = value;

            return Task.CompletedTask;
        });

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public async Task NotExecuteAsyncSideEffectAndReturnOriginalResultWhenTappingErrorResultAsync()
    {
        var result = new Result<int, string>.Error("error");
        var sideEffectValue = 0;

        Result<int, string> tapped = await result.TapAsync(value =>
        {
            sideEffectValue = value;

            return Task.CompletedTask;
        });

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public async Task ExecuteAsyncSideEffectAndReturnOriginalResultWhenTappingErrorOnErrorResult()
    {
        var result = new Result<string, int>.Error(42);
        var sideEffectValue = 0;

        Result<string, int> tapped = await result.TapErrorAsync(value =>
        {
            sideEffectValue = value;

            return Task.CompletedTask;
        });

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public async Task NotExecuteAsyncSideEffectAndReturnOriginalResultWhenTappingErrorOnSuccessResult()
    {
        var result = new Result<string, int>.Ok("success");
        var sideEffectValue = 0;

        Result<string, int> tapped = await result.TapErrorAsync(value =>
        {
            sideEffectValue = value;

            return Task.CompletedTask;
        });

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBeSameAs(result);
    }

    [Fact]
    public async Task ExecuteSideEffectAndReturnOriginalResultWhenTappingSuccessTaskResultAsync()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));
        var sideEffectValue = 0;

        Result<int, string> tapped = await resultTask.TapAsync(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task NotExecuteSideEffectAndReturnOriginalResultWhenTappingErrorTaskResultAsync()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("error"));
        var sideEffectValue = 0;

        Result<int, string> tapped = await resultTask.TapAsync(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task ExecuteSideEffectAndReturnOriginalResultWhenTappingErrorOnErrorTaskResult()
    {
        Task<Result<string, int>> resultTask = Task.FromResult<Result<string, int>>(new Result<string, int>.Error(42));
        var sideEffectValue = 0;

        Result<string, int> tapped = await resultTask.TapErrorAsync(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task NotExecuteSideEffectAndReturnOriginalResultWhenTappingErrorOnSuccessTaskResult()
    {
        Task<Result<string, int>> resultTask = Task.FromResult<Result<string, int>>(new Result<string, int>.Ok("success"));
        var sideEffectValue = 0;

        Result<string, int> tapped = await resultTask.TapErrorAsync(value => sideEffectValue = value);

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task ExecuteAsyncSideEffectAndReturnOriginalResultWhenTappingSuccessTaskResultAsync()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));
        var sideEffectValue = 0;

        Result<int, string> tapped = await resultTask.TapAsync(value =>
        {
            sideEffectValue = value;

            return Task.CompletedTask;
        });

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task NotExecuteAsyncSideEffectAndReturnOriginalResultWhenTappingErrorTaskResultAsync()
    {
        Task<Result<int, string>> resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("error"));
        var sideEffectValue = 0;

        Result<int, string> tapped = await resultTask.TapAsync(value =>
        {
            sideEffectValue = value;

            return Task.CompletedTask;
        });

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task ExecuteAsyncSideEffectAndReturnOriginalResultWhenTappingErrorOnErrorTaskResult()
    {
        Task<Result<string, int>> resultTask = Task.FromResult<Result<string, int>>(new Result<string, int>.Error(42));
        var sideEffectValue = 0;

        Result<string, int> tapped = await resultTask.TapErrorAsync(value =>
        {
            sideEffectValue = value;

            return Task.CompletedTask;
        });

        sideEffectValue.ShouldBe(42);
        tapped.ShouldBe(await resultTask);
    }

    [Fact]
    public async Task NotExecuteAsyncSideEffectAndReturnOriginalResultWhenTappingErrorOnSuccessTaskResult()
    {
        Task<Result<string, int>> resultTask = Task.FromResult<Result<string, int>>(new Result<string, int>.Ok("success"));
        var sideEffectValue = 0;

        Result<string, int> tapped = await resultTask.TapErrorAsync(value =>
        {
            sideEffectValue = value;

            return Task.CompletedTask;
        });

        sideEffectValue.ShouldBe(0);
        tapped.ShouldBe(await resultTask);
    }
}

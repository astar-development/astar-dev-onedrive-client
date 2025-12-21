namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class TryExtensionsShould
{
    [Fact]
    public void MapExceptionToErrorResponseUsingBaseExceptionMessage()
    {
        var inner = new InvalidOperationException("root cause");
        var ex = new Exception("wrapper", inner);
        Result<int, Exception> err = new Result<int, Exception>.Error(ex);

        Result<int, ErrorResponse> mapped = err.ToErrorResponse();

        mapped.ShouldBeOfType<Result<int, ErrorResponse>.Error>()
            .Reason.Message.ShouldBe("root cause");
    }

    [Fact]
    public void PassThroughSuccessUnchanged()
    {
        Result<string, Exception> ok = new Result<string, Exception>.Ok("value");

        Result<string, ErrorResponse> mapped = ok.ToErrorResponse();

        mapped.ShouldBeOfType<Result<string, ErrorResponse>.Ok>()
            .Value.ShouldBe("value");
    }

    [Fact]
    public async Task MapExceptionToErrorResponseAsyncUsingBaseExceptionMessage()
    {
        var inner = new ApplicationException("base message");
        var ex = new Exception("outer", inner);
        Task<Result<bool, Exception>> task = Task.FromResult<Result<bool, Exception>>(new Result<bool, Exception>.Error(ex));

        Result<bool, ErrorResponse> mapped = await task.ToErrorResponseAsync();

        mapped.ShouldBeOfType<Result<bool, ErrorResponse>.Error>()
            .Reason.Message.ShouldBe("base message");
    }

    [Fact]
    public async Task PassThroughSuccessUnchangedAsync()
    {
        Task<Result<int, Exception>> task = Task.FromResult<Result<int, Exception>>(new Result<int, Exception>.Ok(42));

        Result<int, ErrorResponse> mapped = await task.ToErrorResponseAsync();

        mapped.ShouldBeOfType<Result<int, ErrorResponse>.Ok>()
            .Value.ShouldBe(42);
    }
}
